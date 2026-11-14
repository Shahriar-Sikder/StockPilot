// The mockup's M / H / SING / MF tables for the reference lists, mapped onto the ABP app services.
// Every list service has GetAll(GridInput) / GetForEdit / Create / Update / Delete / Restore / GetForCombo;
// ItemCategory and ItemSubCategory also have UploadPicture (base64 in, the saved PictureUrl out) / DeletePicture;
// the picture itself is loaded straight from its PictureUrl.
//
// field: { key, label, type: 'text' | 'num' | 'date' | 'bool' | 'img' | 'fk', required?, default?,
//          fk?: combo service, col?: list DTO property shown in the grid, sortKey?: server sort field }
// Foreign keys show the code for currency, unit and country and the name otherwise (the mockup's lbl()).

import { isGranted } from '../abp.js';
import { FEATURES, isFeatureEnabled } from '../inventory-shared/features.js';

// Each setup list has its own permissions: Pages.StockPilot.Setup.<List> with .Create / .Edit / .Delete.
const setupPermissions = (list) => {
  const permission = `Pages.StockPilot.Setup.${list}`;
  return { permission, canCreate: `${permission}.Create`, canEdit: `${permission}.Edit`, canDelete: `${permission}.Delete` };
};
const SUPPLIERS = { permission: 'Pages.StockPilot.Suppliers', canCreate: 'Pages.StockPilot.Suppliers.Create', canEdit: 'Pages.StockPilot.Suppliers.Edit', canDelete: 'Pages.StockPilot.Suppliers.Delete' };
export const CODE_LABEL = new Set(['Currency', 'UnitMaster', 'Country']);

export const LISTS = {
  itemCategory: {
    service: 'ItemCategory', tag: 'ItemCategory', title: 'Categories', ...setupPermissions('Categories'), picture: true,
    add: 'Add category', edit: 'Edit category', save: 'Save category', saved: 'Saved category', updated: 'Updated category',
    fields: [
      { key: 'code', label: 'Code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'pictureUrl', label: 'Picture', type: 'img' }
    ]
  },
  itemSubCategory: {
    service: 'ItemSubCategory', tag: 'ItemSubCategory', title: 'Sub-categories', ...setupPermissions('SubCategories'), picture: true,
    add: 'Add sub-category', edit: 'Edit sub-category', save: 'Save sub-category', saved: 'Saved sub-category', updated: 'Updated sub-category',
    fields: [
      { key: 'itemCategoryId', label: 'Category', type: 'fk', fk: 'ItemCategory', col: 'itemCategoryName', sortKey: 'itemCategoryName', required: true },
      { key: 'code', label: 'Code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'pictureUrl', label: 'Picture', type: 'img' }
    ],
    filters: [{ key: 'itemCategoryId', all: 'All categories', fk: 'ItemCategory' }]
  },
  unitSet: {
    service: 'UnitSet', tag: 'UnitSet', title: 'Unit groups', ...setupPermissions('UnitSets'),
    add: 'Add unit group', edit: 'Edit unit group', save: 'Save unit group', saved: 'Saved unit group', updated: 'Updated unit group',
    fields: [
      { key: 'code', label: 'Code', required: true },
      { key: 'name', label: 'Name', required: true }
    ]
  },
  unitMaster: {
    service: 'UnitMaster', tag: 'UnitMaster', title: 'Units', ...setupPermissions('Units'),
    add: 'Add unit', edit: 'Edit unit', save: 'Save unit', saved: 'Saved unit', updated: 'Updated unit',
    fields: [
      { key: 'unitSetId', label: 'Unit group', type: 'fk', fk: 'UnitSet', col: 'unitSetName', sortKey: 'unitSetName', required: true },
      { key: 'code', label: 'Code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'isBaseUnit', label: 'Base unit', type: 'bool' }
    ],
    filters: [{ key: 'unitSetId', all: 'All unit groups', fk: 'UnitSet' }]
  },
  unitConversion: {
    service: 'UnitConversion', tag: 'UnitConversion', title: 'Unit conversions', ...setupPermissions('UnitConversions'),
    add: 'Add unit conversion', edit: 'Edit unit conversion', save: 'Save unit conversion', saved: 'Saved unit conversion', updated: 'Updated unit conversion',
    fields: [
      { key: 'fromUnitId', label: '1 of', type: 'fk', fk: 'UnitMaster', col: 'fromUnitCode', sortKey: 'fromUnitCode', required: true },
      { key: 'toUnitId', label: 'equals (in)', type: 'fk', fk: 'UnitMaster', col: 'toUnitCode', sortKey: 'toUnitCode', required: true },
      { key: 'factor', label: 'Factor', type: 'num', required: true }
    ],
    filters: [{ key: 'fromUnitId', all: 'Any from unit', fk: 'UnitMaster' }, { key: 'toUnitId', all: 'Any to unit', fk: 'UnitMaster' }]
  },
  currency: {
    service: 'Currency', tag: 'Currency', title: 'Currencies', ...setupPermissions('Currencies'),
    add: 'Add currency', edit: 'Edit currency', save: 'Save currency', saved: 'Saved currency', updated: 'Updated currency',
    fields: [
      { key: 'code', label: 'Code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'symbol', label: 'Symbol' },
      { key: 'decimalPlaces', label: 'Decimals', type: 'num', default: 2 },
      { key: 'isBaseCurrency', label: 'Base currency', type: 'bool' }
    ]
  },
  currencyConversion: {
    service: 'CurrencyConversion', tag: 'CurrencyConversion', title: 'Exchange rates', ...setupPermissions('CurrencyConversions'),
    add: 'Add exchange rate', edit: 'Edit exchange rate', save: 'Save exchange rate', saved: 'Saved exchange rate', updated: 'Updated exchange rate',
    fields: [
      { key: 'fromCurrencyId', label: 'From', type: 'fk', fk: 'Currency', col: 'fromCurrencyCode', sortKey: 'fromCurrencyCode', required: true },
      { key: 'toCurrencyId', label: 'To', type: 'fk', fk: 'Currency', col: 'toCurrencyCode', sortKey: 'toCurrencyCode', required: true },
      { key: 'rate', label: 'Rate', type: 'num', required: true },
      { key: 'effectiveDate', label: 'Effective date', type: 'date', required: true }
    ],
    filters: [{ key: 'fromCurrencyId', all: 'Any from currency', fk: 'Currency' }, { key: 'toCurrencyId', all: 'Any to currency', fk: 'Currency' }]
  },
  country: {
    service: 'Country', tag: 'Country', title: 'Countries', ...setupPermissions('Countries'),
    add: 'Add country', edit: 'Edit country', save: 'Save country', saved: 'Saved country', updated: 'Updated country',
    fields: [
      { key: 'code', label: 'Code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'phoneCode', label: 'Phone code' }
    ]
  },
  supplier: {
    service: 'Supplier', tag: 'Supplier', title: 'Suppliers', ...SUPPLIERS, wide: true,
    add: 'Add supplier', edit: 'Edit supplier', save: 'Save supplier', saved: 'Saved supplier', updated: 'Updated supplier',
    fields: [
      { key: 'code', label: 'Code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'contactPerson', label: 'Contact' },
      { key: 'phone', label: 'Phone' },
      { key: 'countryId', label: 'Country', type: 'fk', fk: 'Country', col: 'countryName', sortKey: 'countryName' },
      { key: 'currencyId', label: 'Currency', type: 'fk', fk: 'Currency', col: 'currencyCode', sortKey: 'currencyCode' },
      { key: 'paymentTermDays', label: 'Payment days', type: 'num' }
    ]
  },
  // Only the add / edit dialog is shared: #/items has its own grid and drawer (../items/index.jsx).
  itemMaster: {
    service: 'ItemMaster', tag: 'ItemMaster', title: 'Items', picture: true, wide: true,
    permission: 'Pages.StockPilot.Items', canCreate: 'Pages.StockPilot.Items.Create', canEdit: 'Pages.StockPilot.Items.Edit', canDelete: 'Pages.StockPilot.Items.Delete',
    add: 'Add item', edit: 'Edit item', save: 'Save item', saved: 'Saved item', updated: 'Updated item',
    fields: [
      { key: 'itemCode', label: 'Code', required: true },
      { key: 'itemName', label: 'Name', required: true },
      { key: 'itemCategoryId', label: 'Category', type: 'fk', fk: 'ItemCategory', required: true },
      { key: 'itemSubCategoryId', label: 'Sub-category', type: 'fk', fk: 'ItemSubCategory', filterBy: { key: 'itemCategoryId', prop: 'itemCategoryId' } },
      { key: 'baseUnitId', label: 'Base unit', type: 'fk', fk: 'UnitMaster', required: true },
      { key: 'standardCost', label: 'Cost price', type: 'num' },
      { key: 'salePrice', label: 'Sale price', type: 'num' },
      { key: 'reorderLevel', label: 'Reorder level', type: 'num' },
      { key: 'pictureUrl', label: 'Picture', type: 'img' }
    ],
    // The form picks a base unit; the item's unit group is that unit's group.
    beforeSave(input, combos) {
      const unit = (combos.UnitMaster || []).find(([id]) => id === input.baseUnitId);
      if (unit) input.unitSetId = unit[2].unitSetId;
    }
  }
};

// Setup lists a store feature turns off.
const LIST_FEATURES = { unitConversion: FEATURES.unitConversions, currencyConversion: FEATURES.multiCurrency };

// The Setup page's pills, in the mockup's order.
export const SETUP_TABS = ['itemCategory', 'itemSubCategory', 'unitSet', 'unitMaster', 'unitConversion', 'currency', 'currencyConversion', 'country'];

/** The setup lists this user may open and this store has. */
export const setupTabs = () => SETUP_TABS.filter((key) => isFeatureEnabled(LIST_FEATURES[key]) && isGranted(LISTS[key].permission));

// Without the Pictures feature a list has no picture column or field (one copy per config, so memo deps stay stable).
const withoutPictures = new WeakMap();
export function withPictureFeature(config) {
  if (isFeatureEnabled(FEATURES.pictures) || !config.fields.some((field) => field.type === 'img')) return config;
  if (!withoutPictures.has(config)) withoutPictures.set(config, { ...config, picture: false, fields: config.fields.filter((field) => field.type !== 'img') });
  return withoutPictures.get(config);
}

// Lists with a page of their own (route -> list key). The setup lists live on #/setup.
export const genericCrudPages = [{ route: '/suppliers', list: 'supplier' }];

export const getGenericCrudPage = (route) => genericCrudPages.find((page) => page.route === route);
