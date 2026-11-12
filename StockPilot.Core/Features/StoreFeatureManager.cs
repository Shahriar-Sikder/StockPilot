using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Features;
using Abp.Domain.Repositories;
using Abp.MultiTenancy;
using Abp.Runtime.Caching;
using Abp.UI;
using Abp.UI.Inputs;
using StockPilot.Editions;
using StockPilot.MultiTenancy;

namespace StockPilot.Features
{
	public class StoreFeatureManager : StockPilotDomainServiceBase
	{
		private readonly IFeatureManager _featureManager;

		private readonly TenantManager _tenantManager;

		private readonly IRepository<TenantFeatureSetting, long> _tenantFeatureRepository;

		private readonly ICacheManager _cacheManager;

		private readonly EditionManager _editionManager;

		public IFeatureChecker FeatureChecker { get; set; }

		public StoreFeatureManager(IFeatureManager featureManager, TenantManager tenantManager, IRepository<TenantFeatureSetting, long> tenantFeatureRepository, ICacheManager cacheManager, EditionManager editionManager)
		{
			_editionManager = editionManager;
			_featureManager = featureManager;
			_tenantManager = tenantManager;
			_tenantFeatureRepository = tenantFeatureRepository;
			_cacheManager = cacheManager;
		}

		public virtual IReadOnlyList<Feature> GetAllFeatures()
		{
			List<Feature> ordered = new List<Feature>();
			foreach (Feature item in from f in _featureManager.GetAll()
				where f.Parent == null
				select f)
			{
				Add(item);
			}
			return ordered;
			void Add(Feature feature)
			{
				ordered.Add(feature);
				foreach (Feature child in feature.Children)
				{
					Add(child);
				}
			}
		}

		public virtual List<NameValue> Normalize(IEnumerable<NameValue> values)
		{
			List<NameValue> result = new List<NameValue>();
			foreach (NameValue value in values ?? Enumerable.Empty<NameValue>())
			{
				Feature feature = ((value == null || value.Name == null) ? null : _featureManager.GetOrNull(value.Name));
				if (feature == null)
				{
					throw new UserFriendlyException(L("UnknownFeature", value?.Name));
				}
				string text = (value.Value ?? string.Empty).Trim();
				if (feature.InputType is CheckboxInputType)
				{
					if (!bool.TryParse(text, out var flag))
					{
						throw new UserFriendlyException(L("InvalidFeatureValue", feature.Name, value.Value));
					}
					text = (flag ? "true" : "false");
				}
				else if (feature.InputType?.Validator != null && !feature.InputType.Validator.IsValid(text))
				{
					throw new UserFriendlyException(L("InvalidFeatureValue", feature.Name, value.Value));
				}
				result.RemoveAll((NameValue r) => r.Name == feature.Name);
				result.Add(new NameValue(feature.Name, text));
			}
			return result;
		}

		public virtual void ApplyParentRule(IDictionary<string, string> values)
		{
			foreach (Feature feature in from f in GetAllFeatures()
				where f.Parent != null
				select f)
			{
				if (IsOff(values, feature.Parent) && values.ContainsKey(feature.Name))
				{
					values[feature.Name] = "false";
				}
			}
		}

		public virtual async Task<string> GetEditionValueAsync(int? editionId, Feature feature)
		{
			if (!editionId.HasValue)
			{
				return feature.DefaultValue;
			}
			return (await _editionManager.GetFeatureValueOrNullAsync(editionId.Value, feature.Name)) ?? feature.DefaultValue;
		}

		public virtual async Task<Dictionary<string, string>> GetOverridesAsync(int tenantId)
		{
			return (from f in await _tenantFeatureRepository.GetAllListAsync((TenantFeatureSetting f) => f.TenantId == tenantId)
				group f by f.Name).ToDictionary((IGrouping<string, TenantFeatureSetting> g) => g.Key, (IGrouping<string, TenantFeatureSetting> g) => g.First().Value);
		}

		public virtual async Task SetTenantFeaturesAsync(int tenantId, IEnumerable<NameValue> values)
		{
			Tenant tenant = await _tenantManager.GetByIdAsync(tenantId);
			Dictionary<string, string> overrides = await GetOverridesAsync(tenantId);
			List<NameValue> changes = Normalize(values);
			Dictionary<string, string> effective = new Dictionary<string, string>();
			foreach (Feature feature in GetAllFeatures())
			{
				Dictionary<string, string> dictionary = effective;
				string name = feature.Name;
				string own;
				string value = ((!overrides.TryGetValue(feature.Name, out own)) ? (await GetEditionValueAsync(tenant.EditionId, feature)) : own);
				dictionary[name] = value;
			}
			foreach (NameValue change in changes)
			{
				effective[change.Name] = change.Value;
			}
			ApplyParentRule(effective);
			foreach (Feature feature2 in GetAllFeatures())
			{
				bool num = changes.Any((NameValue c) => c.Name == feature2.Name);
				bool underOffParent = feature2.Parent != null && IsOff(effective, feature2.Parent);
				if (num || underOffParent)
				{
					await _tenantManager.SetFeatureValueAsync(tenant, feature2.Name, effective[feature2.Name]);
				}
			}
			await base.CurrentUnitOfWork.SaveChangesAsync();
			ClearCache(tenantId);
		}

		public virtual async Task ResetTenantFeaturesAsync(int tenantId)
		{
			await _tenantManager.ResetAllFeaturesAsync(tenantId);
			await base.CurrentUnitOfWork.SaveChangesAsync();
			ClearCache(tenantId);
		}

		public virtual void ClearCache(int tenantId)
		{
			_cacheManager.GetCache("AbpZeroTenantFeatures").Remove(tenantId.ToString());
		}

		public virtual async Task<bool> IsEnabledAsync(int tenantId, string featureName)
		{
			Feature feature = _featureManager.Get(featureName);
			for (Feature current = feature; current != null; current = current.Parent)
			{
				if (!(await FeatureChecker.IsEnabledAsync(tenantId, current.Name)))
				{
					return false;
				}
			}
			return true;
		}

		private static bool IsOff(IDictionary<string, string> values, Feature feature)
		{
			for (Feature current = feature; current != null; current = current.Parent)
			{
				if (values.TryGetValue(current.Name, out var value) && string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}
}
