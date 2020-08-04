$(function () {
    $.ajax({
        type: "GET",
        url: "/Products/GetProductCategories/",
        dataType: 'json',
        success: function (data) {

            $('.cate-items').empty();

            if (data.length > 0) {
                $.each(data, function (i, item) {
                    $('.cate-items').append($('<li class="item-search" onclick="SetProductGroupId(event)"></li>').val(item.ProductGroupId).html(item.ProductGroup));
                });
            }



        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert('Error' + textStatus + "/" + errorThrown);
        }
    });

});


function SetProductGroupId(event) {
    $("#ProductGroupId").val(event.currentTarget.value);
}


function searchPoducts() {
    var sortvalue = $("#SortedValues").val();
    var groupId = $("#ProductGroupId").val();
    var pagenumber = $("#pagenumber").val();
    var currentFilter = $("#currentFiltervalue").val();
    var searchstring = $("#searchString").val();
    var pageSize = $("#input-limit :selected").val();
    window.location.href = "/Products/ProductCategories?productGroupId=" + groupId + "&sortOrder=" + sortvalue + "&currentFilter=" + currentFilter + "&searchString=" + searchstring + "&page=" + pagenumber + "&pagesize=" + pageSize;
}
function SearchProductCategory() {
    var productgroupId = $("#ProductGroupId").val();
    var searchstring = $("#text-search").val();
    window.location.href = "/Products/ProductCategories?productGroupId=" + productgroupId + "&searchString=" + searchstring;
}

var searchvalues;
var seeall = true;
$('#text-search').autocomplete({
    minLength: 1,
    source: function (request, response) {
        searchvalues = request.term;
        $.ajax({
            url: '/Products/searchProduct',
            method: 'post',
            data: { searchkey: request.term },
            dataType: 'json',
            success: function (data) {
                seeall = true;
                if (!data.length) {
                    seeall = false;
                    data = [
                        {
                            Name: 'No matches found',
                            Path: '/UploadedFiles/Products/no-image.png'
                        }
                    ];

                }

                response(data);
            },
            error: function (err) {
                alert(err);
            }
        });
    },
    open: function () {
        if (seeall) {
            var $li = $("<li>");
            var $link = $("<a>", {
                href: "/Products/ProductCategories?productGroupId=null&searchString=" + searchvalues,
                class: "see-all"
            }).html("See All Results").appendTo($li);
            $li.appendTo($('.ui-autocomplete'));
        }
    },
    focus: updateTextBox,
    select: updateTextBox
}).autocomplete('instance')._renderItem = function (ul, item) {

    return $('<li>')
        .append("<img style='width: 65px; height:58px;' src=" + item.Path + " alt=" + item.Name + "/>")
        .append("<a href=/Products/ProductCategories?productGroupId=null&searchString=" + item.Name + ">" + item.Name + "</a>").appendTo(ul);
};







function updateTextBox(event, ui) {
    $(this).val(ui.item.Name);
    return false;
}



