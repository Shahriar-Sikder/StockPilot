using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Editions;
using Abp.Application.Features;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Dependency;
using StockPilot.Editions.Dto;
using StockPilot.Features;

namespace StockPilot.Editions
{
	[AbpAuthorize(new string[] { "Pages.Editions" })]
	public class EditionAppService : StockPilotAppServiceBase, IEditionAppService, IApplicationService, ITransientDependency
	{
		private readonly EditionManager _editionManager;

		public StoreFeatureManager StoreFeatureManager { get; set; }

		public EditionAppService(EditionManager editionManager)
		{
			_editionManager = editionManager;
		}

		public async Task<ListResultDto<EditionListDto>> GetEditions()
		{
			return new ListResultDto<EditionListDto>((await _editionManager.Editions.ToListAsync()).MapTo<List<EditionListDto>>());
		}

		[AbpAuthorize(new string[] { "Pages.Editions.Create", "Pages.Editions.Edit" })]
		public async Task<GetEditionForEditOutput> GetEditionForEdit(NullableIdDto input)
		{
			IReadOnlyList<Feature> features = base.FeatureManager.GetAll();
			List<NameValue> featureValues;
			EditionEditDto editionEditDto;
			if (input.Id.HasValue)
			{
				Edition edition = await _editionManager.FindByIdAsync(input.Id.Value);
				featureValues = (await _editionManager.GetFeatureValuesAsync(input.Id.Value)).ToList();
				editionEditDto = edition.MapTo<EditionEditDto>();
			}
			else
			{
				editionEditDto = new EditionEditDto();
				featureValues = features.Select((Feature f) => new NameValue(f.Name, f.DefaultValue)).ToList();
			}
			return new GetEditionForEditOutput
			{
				Edition = editionEditDto,
				Features = (from f in features.MapTo<List<FlatFeatureDto>>()
					orderby f.DisplayName
					select f).ToList(),
				FeatureValues = featureValues.Select((NameValue fv) => new NameValueDto(fv)).ToList()
			};
		}

		[AbpAuthorize(new string[] { "Pages.Editions.Create", "Pages.Editions.Edit" })]
		public async Task CreateOrUpdateEdition(CreateOrUpdateEditionDto input)
		{
			if (!input.Edition.Id.HasValue)
			{
				await CreateEditionAsync(input);
			}
			else
			{
				await UpdateEditionAsync(input);
			}
		}

		[AbpAuthorize(new string[] { "Pages.Editions.Delete" })]
		public async Task DeleteEdition(EntityDto input)
		{
			Edition edition = await _editionManager.GetByIdAsync(input.Id);
			await _editionManager.DeleteAsync(edition);
		}

		public async Task<List<ComboboxItemDto>> GetEditionComboboxItems(int? selectedEditionId = null)
		{
			List<ComboboxItemDto> editionItems = new ListResultDto<ComboboxItemDto>((await _editionManager.Editions.ToListAsync()).Select((Edition e) => new ComboboxItemDto(e.Id.ToString(), e.DisplayName)).ToList()).Items.ToList();
			ComboboxItemDto defaultItem = new ComboboxItemDto("null", L("NotAssigned"));
			editionItems.Insert(0, defaultItem);
			if (selectedEditionId.HasValue)
			{
				ComboboxItemDto selectedEdition = editionItems.FirstOrDefault((ComboboxItemDto e) => e.Value == selectedEditionId.Value.ToString());
				if (selectedEdition != null)
				{
					selectedEdition.IsSelected = true;
				}
			}
			else
			{
				defaultItem.IsSelected = true;
			}
			return editionItems;
		}

		[AbpAuthorize(new string[] { "Pages.Editions.Create" })]
		protected virtual async Task CreateEditionAsync(CreateOrUpdateEditionDto input)
		{
			Edition edition = new Edition(input.Edition.DisplayName);
			await _editionManager.CreateAsync(edition);
			await base.CurrentUnitOfWork.SaveChangesAsync();
			await SetFeatureValues(edition, input.FeatureValues);
		}

		[AbpAuthorize(new string[] { "Pages.Editions.Edit" })]
		protected virtual async Task UpdateEditionAsync(CreateOrUpdateEditionDto input)
		{
			Edition edition = await _editionManager.GetByIdAsync(input.Edition.Id.Value);
			edition.DisplayName = input.Edition.DisplayName;
			await SetFeatureValues(edition, input.FeatureValues);
		}

		private Task SetFeatureValues(Edition edition, List<NameValueDto> featureValues)
		{
			Dictionary<string, string> values = StoreFeatureManager.Normalize(featureValues.Select((NameValueDto fv) => new NameValue(fv.Name, fv.Value))).ToDictionary((NameValue v) => v.Name, (NameValue v) => v.Value);
			StoreFeatureManager.ApplyParentRule(values);
			return _editionManager.SetFeatureValuesAsync(edition.Id, values.Select((KeyValuePair<string, string> v) => new NameValue(v.Key, v.Value)).ToArray());
		}
	}
}
