// need load the moment.js to use these filters. 

(function () {

    appModule.filter('momentFormat', function () {
        return function (date, formatStr) {
            if (!date) {
                return '-';
            }

            return moment(date).format(formatStr);
        };
    })
    .filter('momentFormatPZ', function () {
        return function (date, formatStr) {
            if (!date) {
                return '-';
            }

            return moment.parseZone(date).format(formatStr);
        };
    })
    .filter('momentFormatMonthNameWithNumber', function () {
        return function (date, formatStr) {
            if (!date) {
                return '-';
            }
            return moment(date, 'MM').format(formatStr);
        };
    })
    .filter('fromNow', function () {
        return function (date) {
            return moment(date).fromNow();
        };
    });

})();