// Add your custom JS code here
var PreOdds = PreOdds || {};
PreOdds.Utils = {};

PreOdds.Utils.ShowLoadingModal = function (options) {
    $("#divLoading").show();
    //PreOdds.Utils.HideLoadingModal();
    $("#divLoadMore").hide();
    //var str =
    //    '<div class="modal-body" style="padding:20px;text-align:center"><div style="width:100%;padding:20px;text-align:center"><img src="/images/loading.gif"></div></div>';
    //$("#loadingModal .modal-content").html(str);
    //$('#loadingModal').modal({ backdrop: 'static' });
}

PreOdds.Utils.HideLoadingModal = function () {
    $('#loadingModal').modal('hide');
}


//PreOdds.Utils.ShowErrorNotification = function (message) {
//    toastr.options = {
//        "closeButton": true,
//        "debug": false,
//        "newestOnTop": true,
//        "progressBar": true,
//        "positionClass": "toast-top-right",
//        "preventDuplicates": false,
//        "onclick": null,
//        "showDuration": "300",
//        "hideDuration": "1000",
//        "timeOut": "5000",
//        "extendedTimeOut": "1000",
//        "showEasing": "swing",
//        "hideEasing": "linear",
//        "showMethod": "fadeIn",
//        "hideMethod": "fadeOut"
//    }

//    toastr["error"](message);
//}


PreOdds.Utils.GetCurrentDateString = function () {
    var today = new Date();
    var dd = today.getDate();
    var mm = today.getMonth() + 1; //January is 0!
    var hour = today.getHours();
    var min = today.getMinutes();

    var yyyy = today.getFullYear();
    if (dd < 10) {
        dd = '0' + dd;
    }
    if (mm < 10) {
        mm = '0' + mm;
    }
    return dd + '-' + mm + '-' + yyyy;
}

PreOdds.Utils.GetCurrentDateTimeString = function () {
    var today = new Date();
    var dd = today.getDate();
    var mm = today.getMonth() + 1; //January is 0!
    var hour = today.getHours();
    var minute = today.getMinutes();
    var second = today.getSeconds();


    var yyyy = today.getFullYear();
    if (dd < 10) {
        dd = '0' + dd;
    }
    if (mm < 10) {
        mm = '0' + mm;
    }

    if (hour < 10) {
        hour = '0' + hour;
    }

    if (minute < 10) {
        minute = '0' + minute;
    }

    if (second < 10) {
        second = '0' + second;
    }
    return dd + '/' + mm + '/' + yyyy + '  -  ' + hour + ':' + minute + ':' + second;
}

PreOdds.Utils.SetTimezone = function () {
    var d = new Date();
    $.ajax({
        type: "GET",
        url: "/Home/SetTimeZone",
        data: { 'timeZone': d.getTimezoneOffset() },
    });
}