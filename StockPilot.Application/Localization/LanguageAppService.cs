using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Localization;
using Abp.Localization.Sources;
using Abp.UI;
using StockPilot.Localization.Dto;

namespace StockPilot.Localization
{
	[AbpAuthorize(new string[] { "Pages.Administration.Languages" })]
	public class LanguageAppService : StockPilotAppServiceBase, ILanguageAppService, IApplicationService, ITransientDependency
	{
		private readonly IApplicationLanguageManager _applicationLanguageManager;

		private readonly IApplicationLanguageTextManager _applicationLanguageTextManager;

		private readonly IRepository<ApplicationLanguage> _languageRepository;

		public LanguageAppService(IApplicationLanguageManager applicationLanguageManager, IApplicationLanguageTextManager applicationLanguageTextManager, IRepository<ApplicationLanguage> languageRepository)
		{
			_applicationLanguageManager = applicationLanguageManager;
			_languageRepository = languageRepository;
			_applicationLanguageTextManager = applicationLanguageTextManager;
		}

		public async Task<GetLanguagesOutput> GetLanguages()
		{
			IOrderedEnumerable<ApplicationLanguage> languages = (await _applicationLanguageManager.GetLanguagesAsync(base.AbpSession.TenantId)).OrderBy((ApplicationLanguage l) => l.DisplayName);
			ApplicationLanguage defaultLanguage = await _applicationLanguageManager.GetDefaultLanguageOrNullAsync(base.AbpSession.TenantId);
			return new GetLanguagesOutput(languages.MapTo<List<ApplicationLanguageListDto>>(), (defaultLanguage == null) ? null : defaultLanguage.Name);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Languages.Create", "Pages.Administration.Languages.Edit" })]
		public async Task<GetLanguageForEditOutput> GetLanguageForEdit(NullableIdDto input)
		{
			ApplicationLanguage language = null;
			if (input.Id.HasValue)
			{
				language = await _languageRepository.GetAsync(input.Id.Value);
			}
			GetLanguageForEditOutput output = new GetLanguageForEditOutput();
			output.Language = ((language != null) ? language.MapTo<ApplicationLanguageEditDto>() : new ApplicationLanguageEditDto());
			output.LanguageNames = (from c in CultureInfo.GetCultures(CultureTypes.AllCultures)
				orderby c.DisplayName
				select new ComboboxItemDto(c.Name, c.DisplayName + " (" + c.Name + ")")
				{
					IsSelected = (output.Language.Name == c.Name)
				}).ToList();
			output.Flags = (from f in FamFamFamFlagsHelper.FlagClassNames
				orderby f
				select new ComboboxItemDto(f, FamFamFamFlagsHelper.GetCountryCode(f))
				{
					IsSelected = (output.Language.Icon == f)
				}).ToList();
			return output;
		}

		public async Task CreateOrUpdateLanguage(CreateOrUpdateLanguageInput input)
		{
			if (input.Language.Id.HasValue)
			{
				await UpdateLanguageAsync(input);
			}
			else
			{
				await CreateLanguageAsync(input);
			}
		}

		public async Task DeleteLanguage(EntityDto input)
		{
			ApplicationLanguage language = await _languageRepository.GetAsync(input.Id);
			await _applicationLanguageManager.RemoveAsync(base.AbpSession.TenantId, language.Name);
		}

		public async Task SetDefaultLanguage(SetDefaultLanguageInput input)
		{
			await _applicationLanguageManager.SetDefaultLanguageAsync(base.AbpSession.TenantId, GetCultureInfoByChecking(input.Name).Name);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Languages.ChangeTexts" })]
		public async Task<PagedResultDto<LanguageTextListDto>> GetLanguageTexts(GetLanguageTextsInput input)
		{
			if (input.BaseLanguageName.IsNullOrEmpty())
			{
				ApplicationLanguage defaultLanguage = await _applicationLanguageManager.GetDefaultLanguageOrNullAsync(base.AbpSession.TenantId);
				if (defaultLanguage == null)
				{
					defaultLanguage = (await _applicationLanguageManager.GetLanguagesAsync(base.AbpSession.TenantId)).FirstOrDefault();
					if (defaultLanguage == null)
					{
						throw new ApplicationException("No language found in the application!");
					}
				}
				input.BaseLanguageName = defaultLanguage.Name;
			}
			ILocalizationSource source = base.LocalizationManager.GetSource(input.SourceName);
			CultureInfo baseCulture = CultureInfo.GetCultureInfo(input.BaseLanguageName);
			CultureInfo targetCulture = CultureInfo.GetCultureInfo(input.TargetLanguageName);
			IQueryable<LanguageTextListDto> languageTexts = (from localizedString in source.GetAllStrings()
				select new LanguageTextListDto
				{
					Key = localizedString.Name,
					BaseValue = _applicationLanguageTextManager.GetStringOrNull(base.AbpSession.TenantId, source.Name, baseCulture, localizedString.Name),
					TargetValue = _applicationLanguageTextManager.GetStringOrNull(base.AbpSession.TenantId, source.Name, targetCulture, localizedString.Name, tryDefaults: false)
				}).AsQueryable();
			if (input.TargetValueFilter == "EMPTY")
			{
				languageTexts = languageTexts.Where((LanguageTextListDto s) => s.TargetValue.IsNullOrEmpty());
			}
			if (!input.FilterText.IsNullOrEmpty())
			{
				languageTexts = languageTexts.Where((LanguageTextListDto l) => (l.Key != null && l.Key.IndexOf(input.FilterText, StringComparison.CurrentCultureIgnoreCase) >= 0) || (l.BaseValue != null && l.BaseValue.IndexOf(input.FilterText, StringComparison.CurrentCultureIgnoreCase) >= 0) || (l.TargetValue != null && l.TargetValue.IndexOf(input.FilterText, StringComparison.CurrentCultureIgnoreCase) >= 0));
			}
			int totalCount = languageTexts.Count();
			if (!input.Sorting.IsNullOrEmpty())
			{
				languageTexts = languageTexts.OrderBy(input.Sorting);
			}
			if (input.SkipCount > 0)
			{
				languageTexts = languageTexts.Skip(input.SkipCount);
			}
			if (input.MaxResultCount > 0)
			{
				languageTexts = languageTexts.Take(input.MaxResultCount);
			}
			return new PagedResultDto<LanguageTextListDto>(totalCount, languageTexts.ToList());
		}

		public async Task UpdateLanguageText(UpdateLanguageTextInput input)
		{
			CultureInfo culture = GetCultureInfoByChecking(input.LanguageName);
			ILocalizationSource source = base.LocalizationManager.GetSource(input.SourceName);
			await _applicationLanguageTextManager.UpdateStringAsync(base.AbpSession.TenantId, source.Name, culture, input.Key, input.Value);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Languages.Create" })]
		protected virtual async Task CreateLanguageAsync(CreateOrUpdateLanguageInput input)
		{
			CultureInfo culture = GetCultureInfoByChecking(input.Language.Name);
			await CheckLanguageIfAlreadyExists(culture.Name);
			await _applicationLanguageManager.AddAsync(new ApplicationLanguage(base.AbpSession.TenantId, culture.Name, culture.DisplayName, input.Language.Icon));
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Languages.Edit" })]
		protected virtual async Task UpdateLanguageAsync(CreateOrUpdateLanguageInput input)
		{
			CultureInfo culture = GetCultureInfoByChecking(input.Language.Name);
			await CheckLanguageIfAlreadyExists(culture.Name, input.Language.Id.Value);
			ApplicationLanguage language = await _languageRepository.GetAsync(input.Language.Id.Value);
			language.Name = culture.Name;
			language.DisplayName = culture.DisplayName;
			language.Icon = input.Language.Icon;
			await _applicationLanguageManager.UpdateAsync(base.AbpSession.TenantId, language);
		}

		private CultureInfo GetCultureInfoByChecking(string name)
		{
			try
			{
				return CultureInfo.GetCultureInfo(name);
			}
			catch (CultureNotFoundException ex)
			{
				base.Logger.Warn(ex.ToString(), ex);
				throw new UserFriendlyException(L("InvlalidLanguageCode"));
			}
		}

		private async Task CheckLanguageIfAlreadyExists(string languageName, int? expectedId = null)
		{
			ApplicationLanguage existingLanguage = (await _applicationLanguageManager.GetLanguagesAsync(base.AbpSession.TenantId)).FirstOrDefault((ApplicationLanguage l) => l.Name == languageName);
			if (existingLanguage == null || (expectedId.HasValue && existingLanguage.Id == expectedId.Value))
			{
				return;
			}
			throw new UserFriendlyException(L("ThisLanguageAlreadyExists"));
		}
	}
}
