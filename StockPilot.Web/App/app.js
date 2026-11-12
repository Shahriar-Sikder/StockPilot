angular.module('zamzamerp.filter', [])
.filter('griddropdown', function () {
    return function (input, context) {

        try {

            var map = context.col.colDef.editDropdownOptionsArray;
            var idField = context.col.colDef.editDropdownIdLabel;
            var valueField = context.col.colDef.editDropdownValueLabel;
            var initial = context.row.entity[context.col.field];
            if (typeof map !== "undefined") {
                for (var i = 0; i < map.length; i++) {
                    if (map[i][idField] == input) {
                        return map[i][valueField];
                    }
                }
            } else if (initial) {
                return initial;
            }
            return input;

        } catch (e) {
            context.grid.appScope.log("Error: " + e);
        };
    }
});


/* 'app' MODULE DEFINITION */
var appModule = angular.module("app", [
    "ui.router",
    "ui.bootstrap",
    'ui.utils',
    "ui.jq",
    'ui.grid',
    'ui.grid.pagination',
    'ui.grid.edit',
    'ui.grid.selection',
    'ui.grid.cellNav',
    'ui.grid.autoResize',
    'ui.grid.pagination',
    'ui.select',
    'ngTagsInput',
    'ngQuill',
    "oc.lazyLoad",
    "ngSanitize",
    'angularFileUpload',
    'daterangepicker',
,   'angularMoment',
    'ui.bootstrap.datepicker',
    'frapontillo.bootstrap-switch',
    'zamzamerp.filter',
    'gridster',
    'abp'
]).directive('jqdatepicker', function () {
        return {
            restrict: 'A',
            require: 'ngModel',
            link: function (scope, element, attrs, ngModelCtrl) {
                element.datepicker({
                    dateFormat: 'dd/mm/yy',
                    onSelect: function (date) {
                        var ar = date.split("/");
                        date = new Date(ar[2] + "-" + ar[1] + "-" + ar[0]);
                        ngModelCtrl.$setViewValue(date.getTime());
                        //    scope.course.launchDate = date;
                        scope.$apply();
                    }
                });

            }
        };
    });

/* LAZY LOAD CONFIG */

/* This application does not define any lazy-load yet but you can use $ocLazyLoad to define and lazy-load js/css files.
 * This code configures $ocLazyLoad plug-in for this application.
 * See it's documents for more information: https://github.com/ocombe/ocLazyLoad
 */
appModule.config(['$ocLazyLoadProvider', function ($ocLazyLoadProvider) {
    $ocLazyLoadProvider.config({
        cssFilesInsertBefore: 'ng_load_plugins_before', // load the css files before a LINK element with this ID.
        debug: false,
        events: true,
        modules: []
    });
}]);

/* THEME SETTINGS */
App.setAssetsPath(abp.appPath + 'metronic/assets/');
appModule.factory('settings', ['$rootScope', function ($rootScope) {
    var settings = {
        layout: {
            pageSidebarClosed: false, // sidebar menu state
            pageContentWhite: true, // set page content layout
            pageBodySolid: false, // solid body color state
            pageAutoScrollOnLoad: 1000 // auto scroll to top on page load
        },
        layoutImgPath: App.getAssetsPath() + 'admin/layout4/img/',
        layoutCssPath: App.getAssetsPath() + 'admin/layout4/css/',
        assetsPath: abp.appPath + 'metronic/assets',
        globalPath: abp.appPath + 'metronic/assets/global',
        layoutPath: abp.appPath + 'metronic/assets/layouts/layout4'
    };

    $rootScope.settings = settings;

    return settings;
}]);

/* ROUTE DEFINITIONS */

appModule.config([
    '$stateProvider', '$urlRouterProvider', '$qProvider',
    function ($stateProvider, $urlRouterProvider, $qProvider) {

        // Default route (overrided below if user has permission)
        $urlRouterProvider.otherwise("/welcome");

        //Welcome page
        $stateProvider.state('welcome', {
            url: '/welcome',
            templateUrl: '~/App/common/views/welcome/index.cshtml'
        });

        //COMMON routes

        if (abp.auth.hasPermission('Pages.Administration.Roles')) {
            $stateProvider.state('roles', {
                url: '/roles',
                templateUrl: '~/App/common/views/roles/index.cshtml'
            });
        }

        if (abp.auth.hasPermission('Pages.Administration.Users')) {
            $stateProvider.state('users', {
                url: '/users?filterText',
                templateUrl: '~/App/common/views/users/index.cshtml'
            });
        }

        if (abp.auth.hasPermission('Pages.Administration.Languages')) {
            $stateProvider.state('languages', {
                url: '/languages',
                templateUrl: '~/App/common/views/languages/index.cshtml'
            });

            if (abp.auth.hasPermission('Pages.Administration.Languages.ChangeTexts')) {
                $stateProvider.state('languageTexts', {
                    url: '/languages/texts/:languageName?sourceName&baseLanguageName&targetValueFilter&filterText',
                    templateUrl: '~/App/common/views/languages/texts.cshtml'
                });
            }
        }

        if (abp.auth.hasPermission('Pages.Administration.AuditLogs')) {
            $stateProvider.state('auditLogs', {
                url: '/auditLogs',
                templateUrl: '~/App/common/views/auditLogs/index.cshtml'
            });
        }

        if (abp.auth.hasPermission('Pages.Administration.OrganizationUnits')) {
            $stateProvider.state('organizationUnits', {
                url: '/organizationUnits',
                templateUrl: '~/App/common/views/organizationUnits/index.cshtml'
            });
        }

        $stateProvider.state('notifications', {
            url: '/notifications',
            templateUrl: '~/App/common/views/notifications/index.cshtml'
        });

        //HOST routes

        $stateProvider.state('host', {
            'abstract': true,
            url: '/host',
            template: '<div ui-view class="fade-in-up"></div>'
        });

        if (abp.auth.hasPermission('Pages.Tenants')) {
            $urlRouterProvider.otherwise("/host/tenants"); //Entrance page for the host
            $stateProvider.state('host.tenants', {
                url: '/tenants?filterText',
                templateUrl: '~/App/host/views/tenants/index.cshtml'
            });
        }

        if (abp.auth.hasPermission('Pages.Editions')) {
            $stateProvider.state('host.editions', {
                url: '/editions',
                templateUrl: '~/App/host/views/editions/index.cshtml'
            });
        }

        if (abp.auth.hasPermission('Pages.Subscriptions')) {
            $stateProvider.state('host.subscriptions', {
                url: '/subscriptions',
                templateUrl: '~/App/host/views/subscriptions/index.cshtml'
            });
        }

        if (abp.auth.hasPermission('Pages.Administration.Host.Maintenance')) {
            $stateProvider.state('host.maintenance', {
                url: '/maintenance',
                templateUrl: '~/App/host/views/maintenance/index.cshtml'
            });
        }

        if (abp.auth.hasPermission('Pages.Administration.Host.Settings')) {
            $stateProvider.state('host.settings', {
                url: '/settings',
                templateUrl: '~/App/host/views/settings/index.cshtml'
            });
        }

        

        //TENANT routes

        $stateProvider.state('tenant', {
            'abstract': true,
            url: '/tenant',
            template: '<div ui-view class="fade-in-up"></div>'
        });


        if (abp.auth.hasPermission('Pages.Administration.Tenant.Settings')) {
            $stateProvider.state('tenant.settings', {
                url: '/settings',
                templateUrl: '~/App/tenant/views/settings/index.cshtml'
            });
        }

        if (abp.auth.hasPermission('Pages.Tenant.Addons')) {
            $stateProvider.state('tenant.addons', {
                url: '/addons',
                templateUrl: '~/App/tenant/views/addons/index.cshtml'
            });

            $stateProvider.state("tenant.addonsmsssl", {
                url: "/addons-addonsmsssl/:id",
                templateUrl: "~/App/tenant/views/addons/smsssl/smsssl.cshtml"
            });

            $stateProvider.state("tenant.addoncloudinary", {
                url: "/addons-addoncloudinary/:id",
                templateUrl: "~/App/tenant/views/addons/cloudinary/cloudinary.cshtml"
            });
        }
        

        if (abp.auth.hasPermission('Pages.Tenant.SMSTemplate')) {
            $stateProvider.state('tenant.smstemplate', {
                url: '/smstemplate',
                templateUrl: '~/App/tenant/views/templates/index.cshtml'
            });

            $stateProvider.state("tenant.detailtemplate", {
                url: "/connect-detail-template/:id",
                templateUrl: "~/App/tenant/views/templates/detail.cshtml",
                menu: "Administration.Templates"
            });
        }

        if (abp.auth.hasPermission('Pages.Tenant.StockMaster')) {
            $urlRouterProvider.otherwise("/tenant/stockmaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.stockmaster', {
                url: '/stockmaster',
                templateUrl: '~/App/tenant/views/stockmaster/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.StockInHistory')) {
            $urlRouterProvider.otherwise("/tenant/stockinhistory"); //Entrance page for a tenant
            $stateProvider.state('tenant.stockinhistory', {
                url: '/stockinhistory',
                templateUrl: '~/App/tenant/views/stockinhistory/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.DueCollection')) {
            $urlRouterProvider.otherwise("/tenant/duecollection"); //Entrance page for a tenant
            $stateProvider.state('tenant.duecollection', {
                url: '/duecollection',
                templateUrl: '~/App/tenant/views/duecollection/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.CashBox')) {
            $urlRouterProvider.otherwise("/tenant/cashbox"); //Entrance page for a tenant
            $stateProvider.state('tenant.cashbox', {
                url: '/cashbox',
                templateUrl: '~/App/tenant/views/cashBox/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.QuickSale')) {
            $urlRouterProvider.otherwise("/tenant/quicksale"); //Entrance page for a tenant
            $stateProvider.state('tenant.quicksale', {
                url: '/quicksale',
                templateUrl: '~/App/tenant/views/quickSale/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Capital')) {
            $urlRouterProvider.otherwise("/tenant/capital"); //Entrance page for a tenant
            $stateProvider.state('tenant.capital', {
                url: '/capital',
                templateUrl: '~/App/tenant/views/capital/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.RecycleBin')) {
            $urlRouterProvider.otherwise("/tenant/recyclebin"); //Entrance page for a tenant
            $stateProvider.state('tenant.recyclebin', {
                url: '/recyclebin',
                templateUrl: '~/App/tenant/views/recycleBin/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Marketing')) {
            $urlRouterProvider.otherwise("/tenant/marketing"); //Entrance page for a tenant
            $stateProvider.state('tenant.marketing', {
                url: '/marketing',
                templateUrl: '~/App/tenant/views/marketing/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.ExpiringProducts')) {
            $urlRouterProvider.otherwise("/tenant/expiringproducts"); //Entrance page for a tenant
            $stateProvider.state('tenant.expiringproducts', {
                url: '/expiringproducts',
                templateUrl: '~/App/tenant/views/expiringProducts/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.WarrantyProducts')) {
            $urlRouterProvider.otherwise("/tenant/warrantyproducts"); //Entrance page for a tenant
            $stateProvider.state('tenant.warrantyproducts', {
                url: '/warrantyproducts',
                templateUrl: '~/App/tenant/views/warrantyProducts/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.SalesReturnMaster')) {
            $urlRouterProvider.otherwise("/tenant/salesreturnmaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.salesreturnmaster', {
                url: '/salesreturnmaster',
                templateUrl: '~/App/tenant/views/salesreturnmaster/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Customer')) {
            $urlRouterProvider.otherwise("/tenant/customer"); //Entrance page for a tenant
            $stateProvider.state('tenant.customer', {
                url: '/customer',
                templateUrl: '~/App/tenant/views/customer/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.AiInbox')) {
            $urlRouterProvider.otherwise("/tenant/aiinbox"); //Entrance page for a tenant
            $stateProvider.state('tenant.aiinbox', {
                url: '/aiinbox',
                templateUrl: '~/App/tenant/views/aiInbox/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.ChatChannel')) {
            $urlRouterProvider.otherwise("/tenant/chatchannel"); //Entrance page for a tenant
            $stateProvider.state('tenant.chatchannel', {
                url: '/chatchannel',
                templateUrl: '~/App/tenant/views/chatChannel/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.AiSettings')) {
            $urlRouterProvider.otherwise("/tenant/aisettings"); //Entrance page for a tenant
            $stateProvider.state('tenant.aisettings', {
                url: '/aisettings',
                templateUrl: '~/App/tenant/views/aiSettings/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.CourierCredential')) {
            $urlRouterProvider.otherwise("/tenant/couriercredential"); //Entrance page for a tenant
            $stateProvider.state('tenant.couriercredential', {
                url: '/couriercredential',
                templateUrl: '~/App/tenant/views/courierCredential/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.AiUsage')) {
            $urlRouterProvider.otherwise("/tenant/aiusage"); //Entrance page for a tenant
            $stateProvider.state('tenant.aiusage', {
                url: '/aiusage',
                templateUrl: '~/App/tenant/views/aiUsage/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.AiAuditLog')) {
            $urlRouterProvider.otherwise("/tenant/aiauditlog"); //Entrance page for a tenant
            $stateProvider.state('tenant.aiauditlog', {
                url: '/aiauditlog',
                templateUrl: '~/App/tenant/views/aiAuditLog/index.cshtml'
            });
        };

        //$qProvider settings
        $qProvider.errorOnUnhandledRejections(false);

        if (abp.auth.hasPermission('Pages.Tenant.Category')) {
            $urlRouterProvider.otherwise("/tenant/category"); //Entrance page for a tenant
            $stateProvider.state('tenant.category', {
                url: '/category',
                templateUrl: '~/App/tenant/views/category/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.EntityType')) {
            $urlRouterProvider.otherwise("/tenant/entitytype"); //Entrance page for a tenant
            $stateProvider.state('tenant.entitytype', {
                url: '/entitytype',
                templateUrl: '~/App/tenant/views/entitytype/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.EntityFixedVariables')) {
            $urlRouterProvider.otherwise("/tenant/entityfixedvariables"); //Entrance page for a tenant
            $stateProvider.state('tenant.entityfixedvariables', {
                url: '/entityfixedvariables',
                templateUrl: '~/App/tenant/views/entityfixedvariables/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Unit')) {
            $urlRouterProvider.otherwise("/tenant/unit"); //Entrance page for a tenant
            $stateProvider.state('tenant.unit', {
                url: '/unit',
                templateUrl: '~/App/tenant/views/unit/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Size')) {
            $stateProvider.state('tenant.size', {
                url: '/size',
                templateUrl: '~/App/tenant/views/size/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Color')) {
            $stateProvider.state('tenant.color', {
                url: '/color',
                templateUrl: '~/App/tenant/views/color/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Currency')) {
            $urlRouterProvider.otherwise("/tenant/currency"); //Entrance page for a tenant
            $stateProvider.state('tenant.currency', {
                url: '/currency',
                templateUrl: '~/App/tenant/views/currency/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Company')) {
            $urlRouterProvider.otherwise("/tenant/company"); //Entrance page for a tenant
            $stateProvider.state('tenant.company', {
                url: '/company',
                templateUrl: '~/App/tenant/views/company/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.CompanyEntity')) {
            $urlRouterProvider.otherwise("/tenant/companyentity"); //Entrance page for a tenant
            $stateProvider.state('tenant.companyentity', {
                url: '/companyentity',
                templateUrl: '~/App/tenant/views/companyentity/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Location')) {
            $urlRouterProvider.otherwise("/tenant/location"); //Entrance page for a tenant
            $stateProvider.state('tenant.location', {
                url: '/location',
                templateUrl: '~/App/tenant/views/location/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Country')) {
            $urlRouterProvider.otherwise("/tenant/country"); //Entrance page for a tenant
            $stateProvider.state('tenant.country', {
                url: '/country',
                templateUrl: '~/App/tenant/views/country/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Division')) {
            $urlRouterProvider.otherwise("/tenant/division"); //Entrance page for a tenant
            $stateProvider.state('tenant.division', {
                url: '/division',
                templateUrl: '~/App/tenant/views/division/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.SubCategory')) {
            $urlRouterProvider.otherwise("/tenant/subcategory"); //Entrance page for a tenant
            $stateProvider.state('tenant.subcategory', {
                url: '/subcategory',
                templateUrl: '~/App/tenant/views/subcategory/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Employee')) {
            $urlRouterProvider.otherwise("/tenant/employee"); //Entrance page for a tenant
            $stateProvider.state('tenant.employee', {
                url: '/employee',
                templateUrl: '~/App/tenant/views/employee/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.ItemPriceList')) {
            $urlRouterProvider.otherwise("/tenant/itempricelist"); //Entrance page for a tenant
            $stateProvider.state('tenant.itempricelist', {
                url: '/itempricelist',
                templateUrl: '~/App/tenant/views/itempricelist/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.ItemMaster')) {
            $urlRouterProvider.otherwise("/tenant/itemmaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.itemmaster', {
                url: '/itemmaster',
                templateUrl: '~/App/tenant/views/itemmaster/index.cshtml'
            });

            $stateProvider.state("tenant.itemdetails", {
                url: "/itemmaster-details/:id",
                templateUrl: "~/App/tenant/views/itemmaster/createOrEditItemMasterModal.cshtml"
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.LocalAgent')) {
            $urlRouterProvider.otherwise("/tenant/localagent"); //Entrance page for a tenant
            $stateProvider.state('tenant.localagent', {
                url: '/localagent',
                templateUrl: '~/App/tenant/views/localagent/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Client')) {
            $urlRouterProvider.otherwise("/tenant/client"); //Entrance page for a tenant
            $stateProvider.state('tenant.client', {
                url: '/client',
                templateUrl: '~/App/tenant/views/client/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Manufacturar')) {
            $urlRouterProvider.otherwise("/tenant/manufacturar"); //Entrance page for a tenant
            $stateProvider.state('tenant.manufacturar', {
                url: '/manufacturar',
                templateUrl: '~/App/tenant/views/manufacturar/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.ExpenseHistory')) {
            $urlRouterProvider.otherwise("/tenant/expensehistory"); //Entrance page for a tenant
            $stateProvider.state('tenant.expensehistory', {
                url: '/expensehistory',
                templateUrl: '~/App/tenant/views/account/expensehistory/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Supplier')) {
            $urlRouterProvider.otherwise("/tenant/supplier"); //Entrance page for a tenant
            $stateProvider.state('tenant.supplier', {
                url: '/supplier',
                templateUrl: '~/App/tenant/views/supplier/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.CustomerHistory')) {
            $urlRouterProvider.otherwise("/tenant/customerhistory"); //Entrance page for a tenant
            $stateProvider.state('tenant.customerhistory', {
                url: '/customerhistory',
                templateUrl: '~/App/tenant/views/customerhistory/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.EmployeeAttendance')) {
            $urlRouterProvider.otherwise("/tenant/employeeAttendance"); //Entrance page for a tenant
            $stateProvider.state('tenant.employeeAttendance', {
                url: '/employeeAttendance',
                templateUrl: '~/App/tenant/views/employeeAttendance/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.PaymentOption')) {
            $urlRouterProvider.otherwise("/tenant/paymentoption"); //Entrance page for a tenant
            $stateProvider.state('tenant.paymentoption', {
                url: '/paymentoption',
                templateUrl: '~/App/tenant/views/paymentoption/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.PackageMaster')) {
            $urlRouterProvider.otherwise("/tenant/packagemaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.packagemaster', {
                url: '/packagemaster',
                templateUrl: '~/App/tenant/views/packagemaster/index.cshtml'
            });
        };
        
        if (abp.auth.hasPermission('Pages.Tenant.PurchaseOrderMaster')) {
            $urlRouterProvider.otherwise("/tenant/purchaseordermaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.purchaseordermaster', {
                url: '/purchaseordermaster',
                templateUrl: '~/App/tenant/views/purchaseordermaster/main.cshtml'
            });

            $stateProvider.state('tenant.createPurchaseOrder', {
                url: '/create-purchaseorder/:id',
                templateUrl: '~/App/tenant/views/purchaseordermaster/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.RequsitionMaster')) {
            $urlRouterProvider.otherwise("/tenant/requsitionmaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.requsitionmaster', {
                url: '/requsitionmaster',
                templateUrl: '~/App/tenant/views/requsitionmaster/index.cshtml'
            });
        };
        
        if (abp.auth.hasPermission('Pages.Tenant.ReportSuite')) {
            $urlRouterProvider.otherwise("/tenant/reportsuite"); //Entrance page for a tenant
            $stateProvider.state('tenant.reportsuite', {
                url: '/reportsuite',
                templateUrl: '~/App/tenant/views/reportsuite/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.BarCodePrint')) {
            $urlRouterProvider.otherwise("/tenant/barCodePrint"); //Entrance page for a tenant
            $stateProvider.state('tenant.barCodePrint', {
                url: '/reportsuite',
                templateUrl: '~/App/tenant/views/barcodeprint/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.SalesInvoiceMaster')) {
            $urlRouterProvider.otherwise("/tenant/salesinvoicemaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.salesinvoicemaster', {
                url: '/salesinvoicemaster',
                templateUrl: '~/App/tenant/views/salesinvoicemaster/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.Sales')) {
            $urlRouterProvider.otherwise("/tenant/sales"); //Entrance page for a tenant
            $stateProvider.state('tenant.sales', {
                url: '/sales',
                templateUrl: '~/App/tenant/views/sales/index.cshtml'
            });
            $stateProvider.state('tenant.createSales', {
                url: '/create-sales/:id',
                templateUrl: '~/App/tenant/views/sales/newSale.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.OnlineOrderManagement')) {
            $urlRouterProvider.otherwise("/tenant/onlineordermanagement"); //Entrance page for a tenant
            $stateProvider.state('tenant.onlineordermanagement', {
                url: '/onlineordermanagement                                                                                                                             ',
                templateUrl: '~/App/tenant/views/order/onlineOrderManagement/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.UploadInfo')) {
            $urlRouterProvider.otherwise("/tenant/uploadinfo"); //Entrance page for a tenant
            $stateProvider.state('tenant.uploadinfo', {
                url: '/uploadinfo',
                templateUrl: '~/App/tenant/views/uploadinfo/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.AttendanceInfo')) {
            $urlRouterProvider.otherwise("/tenant/attendanceInfo"); //Entrance page for a tenant
            $stateProvider.state('tenant.attendanceinfo', {
                url: '/attendanceinfo',
                templateUrl: '~/App/tenant/views/attendanceinfo/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.EmpAttendanceManual')) {
            $urlRouterProvider.otherwise("/tenant/empattendancemanual"); //Entrance page for a tenant
            $stateProvider.state('tenant.empattendancemanual', {
                url: '/empattendancemanual',
                templateUrl: '~/App/tenant/views/empattendancemanual/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.AttendanceManualOption')) {
            $urlRouterProvider.otherwise("/tenant/attendancemanualoption"); //Entrance page for a tenant
            $stateProvider.state('tenant.attendancemanualoption', {
                url: '/attendancemanualoption',
                templateUrl: '~/App/tenant/views/attendancemanualoption/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.HolidayCalender')) {
            $urlRouterProvider.otherwise("/tenant/holidaycalender"); //Entrance page for a tenant
            $stateProvider.state('tenant.holidaycalender', {
                url: '/holidaycalender',
                templateUrl: '~/App/tenant/views/holidaycalender/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.EmpAttendanceDeviceData')) {
            $urlRouterProvider.otherwise("/tenant/empattendancedevicedata"); //Entrance page for a tenant
            $stateProvider.state('tenant.empattendancedevicedata', {
                url: '/empattendancedevicedata',
                templateUrl: '~/App/tenant/views/empattendancedevicedata/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.LeaveApplicationInfo')) {
            $urlRouterProvider.otherwise("/tenant/leaveapplicationinfo"); //Entrance page for a tenant
            $stateProvider.state('tenant.leaveapplicationinfo', {
                url: '/leaveapplicationinfo',
                templateUrl: '~/App/tenant/views/leaveapplicationinfo/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.LeavePolicy')) {
            $urlRouterProvider.otherwise("/tenant/leavepolicy"); //Entrance page for a tenant
            $stateProvider.state('tenant.leavepolicy', {
                url: '/leavepolicy',
                templateUrl: '~/App/tenant/views/leavepolicy/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.LeaveType')) {
            $urlRouterProvider.otherwise("/tenant/leavetype"); //Entrance page for a tenant
            $stateProvider.state('tenant.leavetype', {
                url: '/leavetype',
                templateUrl: '~/App/tenant/views/leavetype/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.LeaveSummary')) {
            $urlRouterProvider.otherwise("/tenant/leavesummary"); //Entrance page for a tenant
            $stateProvider.state('tenant.leavesummary', {
                url: '/leavesummary',
                templateUrl: '~/App/tenant/views/leavesummary/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.Department')) {
            $urlRouterProvider.otherwise("/tenant/department"); //Entrance page for a tenant
            $stateProvider.state('tenant.department', {
                url: '/department',
                templateUrl: '~/App/tenant/views/hk/department/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.JobUnit')) {
            $urlRouterProvider.otherwise("/tenant/jobunit"); //Entrance page for a tenant
            $stateProvider.state('tenant.jobunit', {
                url: '/jobunit',
                templateUrl: '~/App/tenant/views/jobunit/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AdvanceEmployeePayment')) {
            $urlRouterProvider.otherwise("/tenant/advanceemployeepayment"); //Entrance page for a tenant
            $stateProvider.state('tenant.advanceemployeepayment', {
                url: '/advanceemployeepayment',
                templateUrl: '~/App/tenant/views/advanceemployeepayment/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.Issue')) {
            $urlRouterProvider.otherwise("/tenant/issue"); //Entrance page for a tenant
            $stateProvider.state('tenant.issue', {
                url: '/issue',
                templateUrl: '~/App/tenant/views/inventory/issue/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.Project')) {
            $urlRouterProvider.otherwise("/tenant/order/project"); //Entrance page for a tenant
            $stateProvider.state('tenant.project', {
                url: '/order/project',
                templateUrl: '~/App/tenant/views/order/project/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.SubProject')) {
            $urlRouterProvider.otherwise("/tenant/order/subproject"); //Entrance page for a tenant
            $stateProvider.state('tenant.subproject', {
                url: '/order/subproject',
                templateUrl: '~/App/tenant/views/order/subproject/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.CostingMaster')) {
            $urlRouterProvider.otherwise("/tenant/order/costingmaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.costingmaster', {
                url: '/order/costingmaster',
                templateUrl: '~/App/tenant/views/order/costingmaster/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.BudgetMaster')) {
            $urlRouterProvider.otherwise("/tenant/order/budgetmaster"); //Entrance page for a tenant
            $stateProvider.state('tenant.budgetmaster', {
                url: '/order/budgetmaster',
                templateUrl: '~/App/tenant/views/order/budgetmaster/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ProjectVsCategory')) {
            $urlRouterProvider.otherwise("/tenant/order/projectvscategory"); //Entrance page for a tenant
            $stateProvider.state('tenant.projectvscategory', {
                url: '/order/projectvscategory',
                templateUrl: '~/App/tenant/views/order/projectvscategory/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.DeliveryAreas')) {
            $urlRouterProvider.otherwise("/tenant/deliveryareas"); //Entrance page for a tenant
            $stateProvider.state('tenant.deliveryareas', {
                url: '/deliveryareas',
                templateUrl: '~/App/tenant/views/deliveryareas/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.CustomerAddresses')) {
            $urlRouterProvider.otherwise("/tenant/customeraddresses"); //Entrance page for a tenant
            $stateProvider.state('tenant.customeraddresses', {
                url: '/customeraddresses',
                templateUrl: '~/App/tenant/views/customeraddresses/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.District')) {
            $urlRouterProvider.otherwise("/tenant/district"); //Entrance page for a tenant
            $stateProvider.state('tenant.district', {
                url: '/district',
                templateUrl: '~/App/tenant/views/district/index.cshtml'
            });
        };

        if (abp.auth.hasPermission('Pages.Tenant.EcommDescription')) {
            $urlRouterProvider.otherwise("/tenant/ecommdescription"); //Entrance page for a tenant
            $stateProvider.state('tenant.ecommdescription', {
                url: '/ecommdescription',
                templateUrl: '~/App/tenant/views/ecommdescription/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.CustomerRequestItem')) {
            $urlRouterProvider.otherwise("/tenant/customerrequestitem"); //Entrance page for a tenant
            $stateProvider.state('tenant.customerrequestitem', {
                url: '/customerrequestitem',
                templateUrl: '~/App/tenant/views/customerrequestitem/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.Bank')) {
            $urlRouterProvider.otherwise("/tenant/bank"); //Entrance page for a tenant
            $stateProvider.state('tenant.bank', {
                url: '/bank',
                templateUrl: '~/App/tenant/views/hk/bank/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountTransactionHistory')) {
            $urlRouterProvider.otherwise("/tenant/accounttransactionhistory"); //Entrance page for a tenant
            $stateProvider.state('tenant.accounttransactionhistory', {
                url: '/accounttransactionhistory',
                templateUrl: '~/App/tenant/views/account/accounttransactionhistory/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.StockTransferInfo')) {
            $urlRouterProvider.otherwise("/tenant/stocktransferinfo"); //Entrance page for a tenant
            $stateProvider.state('tenant.stocktransferinfo', {
                url: '/stocktransferinfo',
                templateUrl: '~/App/tenant/views/inventory/stocktransferinfo/index.cshtml'
            });

            $stateProvider.state('tenant.createStocktransferinfo', {
                url: '/create-stocktransferinfo/:id',
                templateUrl: '~/App/tenant/views/inventory/stocktransferinfo/createOrEditStockTransferInfoModal.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountType')) {
            $urlRouterProvider.otherwise("/tenant/accounttype"); //Entrance page for a tenant
            $stateProvider.state('tenant.accounttype', {
                url: '/accounttype',
                templateUrl: '~/App/tenant/views/account/accounttype/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountInfo')) {
            $urlRouterProvider.otherwise("/tenant/accountinfo"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountinfo', {
                url: '/accountinfo',
                templateUrl: '~/App/tenant/views/account/accountinfo/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ExpenseType')) {
            $urlRouterProvider.otherwise("/tenant/expensetype"); //Entrance page for a tenant
            $stateProvider.state('tenant.expensetype', {
                url: '/expensetype',
                templateUrl: '~/App/tenant/views/account/expenseType/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.IncomeType')) {
            $urlRouterProvider.otherwise("/tenant/incometype"); //Entrance page for a tenant
            $stateProvider.state('tenant.incometype', {
                url: '/incometype',
                templateUrl: '~/App/tenant/views/account/incometype/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.OfficeIncomeInfo')) {
            $urlRouterProvider.otherwise("/tenant/officeincomeinfo"); //Entrance page for a tenant
            $stateProvider.state('tenant.officeincomeinfo', {
                url: '/officeincomeinfo',
                templateUrl: '~/App/tenant/views/account/officeincomeinfo/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountHeadDetails')) {
            $urlRouterProvider.otherwise("/tenant/accountheaddetails"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountheaddetails', {
                url: '/accountheaddetails',
                templateUrl: '~/App/tenant/views/account/accountheaddetails/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountHeadSummary')) {
            $urlRouterProvider.otherwise("/tenant/accountheadsummary"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountheadsummary', {
                url: '/accountheadsummary',
                templateUrl: '~/App/tenant/views/account/accountheadsummary/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountHead')) {
            $urlRouterProvider.otherwise("/tenant/accounthead"); //Entrance page for a tenant
            $stateProvider.state('tenant.accounthead', {
                url: '/accounthead',
                templateUrl: '~/App/tenant/views/account/accounthead/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.DocumentInfo')) {
            $urlRouterProvider.otherwise("/tenant/documentinfo"); //Entrance page for a tenant
            $stateProvider.state('tenant.documentinfo', {
                url: '/documentinfo',
                templateUrl: '~/App/tenant/views/documentinfo/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ShipmentInfo')) {
            $urlRouterProvider.otherwise("/tenant/shipmentinfo"); //Entrance page for a tenant
            $stateProvider.state('tenant.shipmentinfo', {
                url: '/shipmentinfo',
                templateUrl: '~/App/tenant/views/shipmentinfo/index.cshtml'
            });
        };

        //#region Accounting module states
        if (abp.auth.hasPermission('Pages.Tenant.AccountingDashboard')) {
            $urlRouterProvider.otherwise("/tenant/accountingdashboard"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountingdashboard', {
                url: '/accountingdashboard',
                templateUrl: '~/App/tenant/views/accounting/accountingDashboard/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ChartOfAccount')) {
            $urlRouterProvider.otherwise("/tenant/chartofaccount"); //Entrance page for a tenant
            $stateProvider.state('tenant.chartofaccount', {
                url: '/chartofaccount',
                templateUrl: '~/App/tenant/views/accounting/chartOfAccount/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.MainHead')) {
            $urlRouterProvider.otherwise("/tenant/mainhead"); //Entrance page for a tenant
            $stateProvider.state('tenant.mainhead', {
                url: '/mainhead',
                templateUrl: '~/App/tenant/views/accounting/mainHead/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.SubHead')) {
            $urlRouterProvider.otherwise("/tenant/subhead"); //Entrance page for a tenant
            $stateProvider.state('tenant.subhead', {
                url: '/subhead',
                templateUrl: '~/App/tenant/views/accounting/subHead/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.JournalEntry')) {
            $urlRouterProvider.otherwise("/tenant/journalentry"); //Entrance page for a tenant
            $stateProvider.state('tenant.journalentry', {
                url: '/journalentry',
                templateUrl: '~/App/tenant/views/accounting/journalEntry/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ContraVoucher')) {
            $urlRouterProvider.otherwise("/tenant/contravoucher"); //Entrance page for a tenant
            $stateProvider.state('tenant.contravoucher', {
                url: '/contravoucher',
                templateUrl: '~/App/tenant/views/accounting/contraVoucher/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.SubLedger')) {
            $urlRouterProvider.otherwise("/tenant/subledger"); //Entrance page for a tenant
            $stateProvider.state('tenant.subledger', {
                url: '/subledger',
                templateUrl: '~/App/tenant/views/accounting/subLedger/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.SubLedgerStatement')) {
            $urlRouterProvider.otherwise("/tenant/subledgerstatement"); //Entrance page for a tenant
            $stateProvider.state('tenant.subledgerstatement', {
                url: '/subledgerstatement',
                templateUrl: '~/App/tenant/views/accounting/subLedgerStatement/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.DayBook')) {
            $urlRouterProvider.otherwise("/tenant/daybook"); //Entrance page for a tenant
            $stateProvider.state('tenant.daybook', {
                url: '/daybook',
                templateUrl: '~/App/tenant/views/accounting/dayBook/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.GeneralLedger')) {
            $urlRouterProvider.otherwise("/tenant/generalledger"); //Entrance page for a tenant
            $stateProvider.state('tenant.generalledger', {
                url: '/generalledger',
                templateUrl: '~/App/tenant/views/accounting/generalLedger/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.BankAccount')) {
            $urlRouterProvider.otherwise("/tenant/bankaccount"); //Entrance page for a tenant
            $stateProvider.state('tenant.bankaccount', {
                url: '/bankaccount',
                templateUrl: '~/App/tenant/views/accounting/bankAccount/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.CashAccount')) {
            $urlRouterProvider.otherwise("/tenant/cashaccount"); //Entrance page for a tenant
            $stateProvider.state('tenant.cashaccount', {
                url: '/cashaccount',
                templateUrl: '~/App/tenant/views/accounting/cashAccount/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ChequeBook')) {
            $urlRouterProvider.otherwise("/tenant/chequebook"); //Entrance page for a tenant
            $stateProvider.state('tenant.chequebook', {
                url: '/chequebook',
                templateUrl: '~/App/tenant/views/accounting/chequeBook/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountPayment')) {
            $urlRouterProvider.otherwise("/tenant/accountpayment"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountpayment', {
                url: '/accountpayment',
                templateUrl: '~/App/tenant/views/accounting/accountPayment/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.BankReconciliation')) {
            $urlRouterProvider.otherwise("/tenant/bankreconciliation"); //Entrance page for a tenant
            $stateProvider.state('tenant.bankreconciliation', {
                url: '/bankreconciliation',
                templateUrl: '~/App/tenant/views/accounting/bankReconciliation/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.BankBranch')) {
            $urlRouterProvider.otherwise("/tenant/bankbranch"); //Entrance page for a tenant
            $stateProvider.state('tenant.bankbranch', {
                url: '/bankbranch',
                templateUrl: '~/App/tenant/views/accounting/bankBranch/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.PurchaseJournal')) {
            $urlRouterProvider.otherwise("/tenant/purchasejournal"); //Entrance page for a tenant
            $stateProvider.state('tenant.purchasejournal', {
                url: '/purchasejournal',
                templateUrl: '~/App/tenant/views/accounting/purchaseJournal/index.cshtml'
            });
            $stateProvider.state('tenant.purchasejournalform', {
                url: '/purchasejournal/form/:id',
                templateUrl: '~/App/tenant/views/accounting/purchaseJournal/createOrEdit.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.IssueSalesJournal')) {
            $urlRouterProvider.otherwise("/tenant/issuesalesjournal"); //Entrance page for a tenant
            $stateProvider.state('tenant.issuesalesjournal', {
                url: '/issuesalesjournal',
                templateUrl: '~/App/tenant/views/accounting/issueSalesJournal/index.cshtml'
            });
            $stateProvider.state('tenant.issuesalesjournalform', {
                url: '/issuesalesjournal/form/:id',
                templateUrl: '~/App/tenant/views/accounting/issueSalesJournal/createOrEdit.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.APAllocation')) {
            $urlRouterProvider.otherwise("/tenant/apallocation"); //Entrance page for a tenant
            $stateProvider.state('tenant.apallocation', {
                url: '/apallocation?paymentId',
                templateUrl: '~/App/tenant/views/accounting/apAllocation/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.PaymentTerm')) {
            $urlRouterProvider.otherwise("/tenant/paymentterm"); //Entrance page for a tenant
            $stateProvider.state('tenant.paymentterm', {
                url: '/paymentterm',
                templateUrl: '~/App/tenant/views/accounting/paymentTerm/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.CostCenter')) {
            $urlRouterProvider.otherwise("/tenant/costcenter"); //Entrance page for a tenant
            $stateProvider.state('tenant.costcenter', {
                url: '/costcenter',
                templateUrl: '~/App/tenant/views/accounting/costCenter/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ProfitCenter')) {
            $urlRouterProvider.otherwise("/tenant/profitcenter"); //Entrance page for a tenant
            $stateProvider.state('tenant.profitcenter', {
                url: '/profitcenter',
                templateUrl: '~/App/tenant/views/accounting/profitCenter/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.FixedAsset')) {
            $urlRouterProvider.otherwise("/tenant/fixedasset"); //Entrance page for a tenant
            $stateProvider.state('tenant.fixedasset', {
                url: '/fixedasset',
                templateUrl: '~/App/tenant/views/accounting/fixedAsset/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AssetCategory')) {
            $urlRouterProvider.otherwise("/tenant/assetcategory"); //Entrance page for a tenant
            $stateProvider.state('tenant.assetcategory', {
                url: '/assetcategory',
                templateUrl: '~/App/tenant/views/accounting/assetCategory/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AssetDepreciation')) {
            $urlRouterProvider.otherwise("/tenant/assetdepreciation"); //Entrance page for a tenant
            $stateProvider.state('tenant.assetdepreciation', {
                url: '/assetdepreciation',
                templateUrl: '~/App/tenant/views/accounting/assetDepreciation/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AssetDisposal')) {
            $urlRouterProvider.otherwise("/tenant/assetdisposal"); //Entrance page for a tenant
            $stateProvider.state('tenant.assetdisposal', {
                url: '/assetdisposal',
                templateUrl: '~/App/tenant/views/accounting/assetDisposal/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AssetPurchase')) {
            $urlRouterProvider.otherwise("/tenant/assetpurchase"); //Entrance page for a tenant
            $stateProvider.state('tenant.assetpurchase', {
                url: '/assetpurchase',
                templateUrl: '~/App/tenant/views/accounting/assetPurchase/index.cshtml'
            });
            $stateProvider.state('tenant.assetpurchaseform', {
                url: '/assetpurchase/form/:id',
                templateUrl: '~/App/tenant/views/accounting/assetPurchase/createOrEdit.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountBudget')) {
            $urlRouterProvider.otherwise("/tenant/accountbudget"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountbudget', {
                url: '/accountbudget',
                templateUrl: '~/App/tenant/views/accounting/accountBudget/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ExpenseAllocation')) {
            $urlRouterProvider.otherwise("/tenant/expenseallocation"); //Entrance page for a tenant
            $stateProvider.state('tenant.expenseallocation', {
                url: '/expenseallocation',
                templateUrl: '~/App/tenant/views/accounting/expenseAllocation/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountingReports')) {
            $urlRouterProvider.otherwise("/tenant/accountingreports"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountingreports', {
                url: '/accountingreports',
                templateUrl: '~/App/tenant/views/accounting/accountingReports/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.FinancialYear')) {
            $urlRouterProvider.otherwise("/tenant/financialyear"); //Entrance page for a tenant
            $stateProvider.state('tenant.financialyear', {
                url: '/financialyear',
                templateUrl: '~/App/tenant/views/accounting/financialYear/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.FinancialPeriod')) {
            $urlRouterProvider.otherwise("/tenant/financialperiod"); //Entrance page for a tenant
            $stateProvider.state('tenant.financialperiod', {
                url: '/financialperiod',
                templateUrl: '~/App/tenant/views/accounting/financialPeriod/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AutoPostingRule')) {
            $urlRouterProvider.otherwise("/tenant/autopostingrule"); //Entrance page for a tenant
            $stateProvider.state('tenant.autopostingrule', {
                url: '/autopostingrule',
                templateUrl: '~/App/tenant/views/accounting/autoPostingRule/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.GLMapping')) {
            $urlRouterProvider.otherwise("/tenant/glmapping"); //Entrance page for a tenant
            $stateProvider.state('tenant.glmapping', {
                url: '/glmapping',
                templateUrl: '~/App/tenant/views/accounting/glMapping/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.PostingTemplate')) {
            $urlRouterProvider.otherwise("/tenant/postingtemplate"); //Entrance page for a tenant
            $stateProvider.state('tenant.postingtemplate', {
                url: '/postingtemplate',
                templateUrl: '~/App/tenant/views/accounting/postingTemplate/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.ExchangeRate')) {
            $urlRouterProvider.otherwise("/tenant/exchangerate"); //Entrance page for a tenant
            $stateProvider.state('tenant.exchangerate', {
                url: '/exchangerate',
                templateUrl: '~/App/tenant/views/accounting/exchangeRate/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.InterCompanyTransaction')) {
            $urlRouterProvider.otherwise("/tenant/intercompanytransaction"); //Entrance page for a tenant
            $stateProvider.state('tenant.intercompanytransaction', {
                url: '/intercompanytransaction',
                templateUrl: '~/App/tenant/views/accounting/interCompanyTransaction/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountSetup')) {
            $urlRouterProvider.otherwise("/tenant/accountsetup"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountsetup', {
                url: '/accountsetup',
                templateUrl: '~/App/tenant/views/accounting/accountSetup/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.PaymentOptionGLMap')) {
            $stateProvider.state('tenant.paymentoptionglmap', {
                url: '/paymentoptionglmap',
                templateUrl: '~/App/tenant/views/accounting/paymentOptionGLMap/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountOpeningBalance')) {
            $urlRouterProvider.otherwise("/tenant/accountopeningbalance"); //Entrance page for a tenant
            $stateProvider.state('tenant.accountopeningbalance', {
                url: '/accountopeningbalance',
                templateUrl: '~/App/tenant/views/accounting/accountOpeningBalance/index.cshtml'
            });
        };
        if (abp.auth.hasPermission('Pages.Tenant.AccountTransactionType')) {
            $urlRouterProvider.otherwise("/tenant/accounttransactiontype"); //Entrance page for a tenant
            $stateProvider.state('tenant.accounttransactiontype', {
                url: '/accounttransactiontype',
                templateUrl: '~/App/tenant/views/accounting/accountTransactionType/index.cshtml'
            });
        };
        //#endregion Accounting module states


            // ===== MaterialManagement routes =====
            if (abp.auth.hasPermission('Pages.Tenant.CostCenterDeptMap')) { //Entrance page for a tenant
                $stateProvider.state('tenant.costcenterdeptmap', {
                    url: '/costcenterdeptmap',
                    templateUrl: '~/App/tenant/views/materialManagement/costCenterDeptMap/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.UserStoreMap')) { //Entrance page for a tenant
                $stateProvider.state('tenant.userstoremap', {
                    url: '/userstoremap',
                    templateUrl: '~/App/tenant/views/materialManagement/userStoreMap/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.SystemConfiguration')) { //Entrance page for a tenant
                $stateProvider.state('tenant.systemconfiguration', {
                    url: '/systemconfiguration',
                    templateUrl: '~/App/tenant/views/materialManagement/hk/systemconfiguration/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.Store')) { //Entrance page for a tenant
                $stateProvider.state('tenant.store', {
                    url: '/store',
                    templateUrl: '~/App/tenant/views/materialManagement/hk/store/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.Property')) { //Entrance page for a tenant
                $stateProvider.state('tenant.property', {
                    url: '/property',
                    templateUrl: '~/App/tenant/views/materialManagement/hk/property/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.NotificationTemplate')) { //Entrance page for a tenant
                $stateProvider.state('tenant.notificationtemplate', {
                    url: '/notificationtemplate',
                    templateUrl: '~/App/tenant/views/materialManagement/hk/notificationtemplate/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.ItemLocation')) { //Entrance page for a tenant
                $stateProvider.state('tenant.itemlocation', {
                    url: '/itemlocation',
                    templateUrl: '~/App/tenant/views/materialManagement/hk/itemlocation/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.Brand')) { //Entrance page for a tenant
                $stateProvider.state('tenant.brand', {
                    url: '/brand',
                    templateUrl: '~/App/tenant/views/materialManagement/hk/brand/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.MaterialIndent')) { //Entrance page for a tenant
                $stateProvider.state('tenant.indentsearchmodal', {
                    url: '/indentsearchmodal',
                    templateUrl: '~/App/tenant/views/materialManagement/indent/indentSearchModal.cshtml'
                });
                $stateProvider.state('tenant.indent', {
                    url: '/indent/:indentId',
                    templateUrl: '~/App/tenant/views/materialManagement/indent/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.IndentTemplate')) { //Entrance page for a tenant
                $stateProvider.state('tenant.searchindenttemplate', {
                    url: '/searchindenttemplate',
                    templateUrl: '~/App/tenant/views/materialManagement/indenttemplate/searchIndentTemplate.cshtml'
                });
                $stateProvider.state('tenant.indenttemplate', {
                    url: '/indenttemplate/:indentTemplateId',
                    templateUrl: '~/App/tenant/views/materialManagement/indenttemplate/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.StockTransfer')) { //Entrance page for a tenant
                $stateProvider.state('tenant.searchstocktransfer', {
                    url: '/searchstocktransfer',
                    templateUrl: '~/App/tenant/views/materialManagement/stockTransfer/searchStockTransfer.cshtml'
                });
                $stateProvider.state('tenant.stocktransfer', {
                    url: '/stocktransfer/:stockTransferId',
                    templateUrl: '~/App/tenant/views/materialManagement/stockTransfer/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.StockAdjustment')) { //Entrance page for a tenant
                $stateProvider.state('tenant.searchstockadjustment', {
                    url: '/searchstockadjustment',
                    templateUrl: '~/App/tenant/views/materialManagement/stockadjustment/searchStockAdjustment.cshtml'
                });
                $stateProvider.state('tenant.stockadjustment', {
                    url: '/stockadjustment/:stockAdjustmentId',
                    templateUrl: '~/App/tenant/views/materialManagement/stockadjustment/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.MaterialIssue')) { //Entrance page for a tenant
                $stateProvider.state('tenant.searchmaterialissue', {
                    url: '/materialissue',
                    templateUrl: '~/App/tenant/views/materialManagement/issue/searchIssue.cshtml'
                });
                $stateProvider.state('tenant.materialissue', {
                    url: '/materialissue/:issueId',
                    templateUrl: '~/App/tenant/views/materialManagement/issue/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.MaterialPurchaseOrder')) { //Entrance page for a tenant
                $stateProvider.state('tenant.searchpurchaseorder', {
                    url: '/searchpurchaseorder',
                    templateUrl: '~/App/tenant/views/materialManagement/purchaseOrder/searchPurchaseOrder.cshtml'
                });
                $stateProvider.state('tenant.purchaseorder', {
                    url: '/purchaseorder/:purchaseOrderId',
                    templateUrl: '~/App/tenant/views/materialManagement/purchaseorder/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.Requisition')) { //Entrance page for a tenant
                $stateProvider.state('tenant.searchrequisition', {
                    url: '/searchrequisition',
                    templateUrl: '~/App/tenant/views/materialManagement/requisition/searchRequisition.cshtml'
                });
                $stateProvider.state('tenant.requisition', {
                    url: '/requisition/:requisitionId',
                    templateUrl: '~/App/tenant/views/materialManagement/requisition/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.StockStatusDashboard')) { //Entrance page for a tenant
                $stateProvider.state('tenant.stockstatusdashboard', {
                    url: '/stockstatusdashboard',
                    templateUrl: '~/App/tenant/views/materialManagement/stockStatusDashboard/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.DayCloseManagement')) { //Entrance page for a tenant
                $stateProvider.state('tenant.dayclosemanagement', {
                    url: '/dayclosemanagement',
                    templateUrl: '~/App/tenant/views/materialManagement/dayCloseManagement/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.StockApprovalDashboard')) { //Entrance page for a tenant
                $stateProvider.state('tenant.stockapprovaldashboard', {
                    url: '/stockapprovaldashboard',
                    templateUrl: '~/App/tenant/views/materialManagement/stockApprovalDashboard/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.GoodsReceipt')) { //Entrance page for a tenant
                $stateProvider.state('tenant.searchgoodsreceipt', {
                    url: '/searchgoodsreceipt',
                    templateUrl: '~/App/tenant/views/materialManagement/goodsReceipt/searchGoodsReceipt.cshtml'
                });
                $stateProvider.state('tenant.goodsreceipt', {
                    url: '/goodsreceipt/:transId/:type',
                    templateUrl: '~/App/tenant/views/materialManagement/goodsReceipt/index.cshtml'
                });
            };
            if (abp.auth.hasPermission('Pages.Tenant.MaterialReports')) { //Entrance page for a tenant
                $stateProvider.state('tenant.materialreport', {
                    url: '/materialreport',
                    templateUrl: '~/App/tenant/views/materialManagement/materialReport/index.cshtml'
                });
            };

        if (abp.auth.hasPermission('Pages.Tenant.Dashboard')) {
            $urlRouterProvider.otherwise("/tenant/overview"); //Entrance page for a tenant
            $stateProvider.state('tenant.overview', {
                url: '/overview',
                templateUrl: '~/App/tenant/views/dashboard/overview.cshtml'
            });
            $stateProvider.state('tenant.dashboard', {
                url: '/dashboard',
                templateUrl: '~/App/tenant/views/dashboard/index.cshtml'
            });
        }
    }
]);

appModule.run(["$rootScope", "settings", "$state", 'i18nService', '$uibModalStack', function ($rootScope, settings, $state, i18nService, $uibModalStack) {
    $rootScope.$state = $state;
    $rootScope.$settings = settings;

    $rootScope.$on('$stateChangeSuccess', function () {
        $uibModalStack.dismissAll();
    });

    //Set Ui-Grid language
    if (i18nService.get(abp.localization.currentCulture.name)) {
        i18nService.setCurrentLang(abp.localization.currentCulture.name);
    } else {
        i18nService.setCurrentLang("en");
    }

    $rootScope.safeApply = function (fn) {
        var phase = this.$root.$$phase;
        if (phase == '$apply' || phase == '$digest') {
            if (fn && (typeof (fn) === 'function')) {
                fn();
            }
        } else {
            this.$apply(fn);
        }
    };
}]);