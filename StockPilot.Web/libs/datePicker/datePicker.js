//(function () {
//    var appDP;
//    appDP = angular.module('datePickerApp',[]);
//    appModule.directive('datePickerApp', function () {
//        return {
//            restrict: 'A',
//            require: 'ngModel',
//            link: function (scope, element, attrs, ngModelCtrl) {
//                element.datepicker({
//                    dateFormat: 'dd/mm/yy',
//                    onSelect: function (date) {
//                        var ar = date.split("/");
//                        date = new Date(ar[2] + "-" + ar[1] + "-" + ar[0]);
//                        ngModelCtrl.$setViewValue(date.getTime());
//                        //    scope.course.launchDate = date;
//                        scope.$apply();
//                    }
//                });

//            }
//        };
//    });

//}).call(this);

//appModule.directive('mydatepicker', function () {
//    return {
//        restrict: 'A',
//        require: 'ngModel',
//        link: function (scope, element, attrs, ngModelCtrl) {
//            element.datepicker({
//                dateFormat: 'mm/dd/yyyy',
//                onSelect: function (date) {
//                    scope.date = date;
//                    scope.$apply();
//                }
//            });
//        }
//    };
//});
//var appModule = angular.module("app", []);
//appModule.directive('jqdatepicker', function () {
//    return {
//        restrict: 'A',
//        require: 'ngModel',
//        link: function (scope, element, attrs, ngModelCtrl) {
//            element.datepicker({
//                dateFormat: 'dd/mm/yy',
//                onSelect: function (date) {
//                    var ar = date.split("/");
//                    date = new Date(ar[2] + "-" + ar[1] + "-" + ar[0]);
//                    ngModelCtrl.$setViewValue(date.getTime());
//                    //    scope.course.launchDate = date;
//                    scope.$apply();
//                }
//            });

//        }
//    };
//});


//appDP.directive('datePickerApp', function () {
//    return {
//        restrict: 'A',
//        require: 'ngModel',
//        link: function (scope, elm, attr, ctrl) {

//            // Format date on load
//            ctrl.$formatters.unshift(function (value) {
//                if (value && moment(value).isValid()) {
//                    return moment(new Date(value)).format('MM/DD/YYYY');
//                }
//                return value;
//            })

//            //Disable Calendar
//            //scope.$watch(attr.ngDisabled, function (newVal) {
//            //    if (newVal === true)
//            //        $(elm).datepicker("disable");
//            //    else
//            //        $(elm).datepicker("enable");
//            //});

//            // Datepicker Settings
//            elm.datepicker({
//                autoSize: true,
//                changeYear: true,
//                changeMonth: true,
//                dateFormat: attr["dateformat"] || 'mm/dd/yy',
//                showOn: 'button',
//                buttonText: '<i class="glyphicon glyphicon-calendar"></i>',
//                onSelect: function (valu) {
//                    scope.$apply(function () {
//                        ctrl.$setViewValue(valu);
//                    });
//                    elm.focus();
//                },

//                beforeShow: function () {
//                    debugger;
//                    if (attr["minDate"] != null)
//                        $(elm).datepicker('option', 'minDate', attr["minDate"]);

//                    if (attr["maxDate"] != null)
//                        $(elm).datepicker('option', 'maxDate', attr["maxDate"]);
//                },


//            });
//        }
//    }
//});