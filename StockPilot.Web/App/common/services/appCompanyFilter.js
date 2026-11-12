(function () {
    // Global company-entity filter shown in the top bar when the accounting module is active.
    // The header controller drives visibility via appMenuModule.selectedModule === 'accounting'.
    // Accounting pages that want to honour this selection should listen to 'app.companyEntity.changed'
    // and read appCompanyFilter.selectedCompanyEntityId on init.
    appModule.factory('appCompanyFilter', [
        function () {
            var STORAGE_KEY = 'zemsCompanyEntityId';

            var service = {
                selectedCompanyEntityId: null,

                setSelected: function (id) {
                    service.selectedCompanyEntityId = id || null;
                    try {
                        if (service.selectedCompanyEntityId) {
                            localStorage.setItem(STORAGE_KEY, service.selectedCompanyEntityId);
                        } else {
                            localStorage.removeItem(STORAGE_KEY);
                        }
                    } catch (e) { }
                    abp.event.trigger('app.companyEntity.changed', service.selectedCompanyEntityId);
                }
            };

            try {
                var saved = localStorage.getItem(STORAGE_KEY);
                if (saved) {
                    service.selectedCompanyEntityId = parseInt(saved, 10) || null;
                }
            } catch (e) { }

            return service;
        }
    ]);
})();
