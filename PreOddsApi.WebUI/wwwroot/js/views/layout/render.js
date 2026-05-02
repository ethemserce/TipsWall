var updateDateTimeInterval;

$(document).ready(function () {
    document.getElementById("currentdatetime").innerHTML = PreOdds.Utils.GetCurrentDateTimeString();
    updateDateTimeInterval = window.setInterval(datetimeUpdate, 1000);
});

function datetimeUpdate() {
    document.getElementById("currentdatetime").innerHTML = PreOdds.Utils.GetCurrentDateTimeString();
}

