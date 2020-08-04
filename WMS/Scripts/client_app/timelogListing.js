$(function () {
    //search timelog
    $(document).on('click', '#searchBtn_TimeLog', function () {
        var storeId = $('#StoresList').val();
        var weekNumber = $('#WeekDaysList').val();
        var yearNumber = $('#YearsList').val();

        //redirect
        window.location.replace("/timelog/tindex/?id=" + storeId + "&weekNumber=" + weekNumber + "&YearsList=" + yearNumber);
    });
});