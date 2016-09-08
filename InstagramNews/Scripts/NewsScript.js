var subscribeLikes = function() {
    $('.lblLike.newItem').on('click', function () {
        var self = $(this);
        var id = this.parentElement.parentElement.id;
        var liked = !(self.children('.heart.icon').hasClass('empty'));
        $.ajax({
            url: "/Home/Like",
            data: { id: id, liked: liked }
        })
            .success(function (data) {
                if (!data.error) {
                    self.toggleClass('red');
                    $(self.children('.heart.icon')).toggleClass('empty');
                }
            });
    });
};

var loadPhotos = function() {
    $('.clPhoto.newItem').each(
        function(index, item) {
            var id = item.id;
            var link = $(item).children().children('.linkPhoto');
            var like = $(item).children().children('.lblLike');
            $.ajax({
                    url: "/Home/GetPhotoData",
                    data: { id: id }
                })
                .success(function(data) {
                    if (!data.error) {
                        $(link).attr('href', data.url);
                        if (data.liked) {
                            $(like).toggleClass('red');
                            $($(like).children('.heart.icon')).toggleClass('empty');
                        }
                        link.attr('data-html', '<div class="ui items"><div class="item"><div class="image"><img src="' + data.medium + '"></div><div class="content"><div class="name">' + data.username + '</div><p class="description">' + data.caption + '</p><div class="extra">' + data.likes + ' likes <br/> ' + data.comments + ' comments</div></div></div></div>');
                    }
                });
        });
};

var loadUsers = function() {
    $('.clUser.newItem').each(
        function(index, item) {
            var id = item.id;
            var img = $(item).children().children('.userImage');
            $.ajax({
                    url: "/Home/GetUserImage",
                    data: { id: id }
                })
                .success(function(data) {
                    if (!data.error) {
                        $(img).attr('src', data.image);
                    }
                });
        });
};

var checkNews = function() {
    $.ajax({
            url: "/Home/GetNewsAfterCount",
            data: { timestamp: $('#lastUpdate').val() === undefined ? 0 : $('#lastUpdate').val() }
        })
        .success(function(data) {
            if (data > 0) {
                $('#newStoriesCount').text(data);
                $('#newStories').show();
                document.title = '(' + data + ') Instagram news';
            }
        });
};

var convertTimestamps = function() {
    $('.timestamp').each(function(index, item) {
        var result;
        var t = new Date(Date.now() - $(item).val() * 1000);
        var date = t.getUTCDate();
        var hours = t.getUTCHours();
        var minutes = t.getUTCMinutes();
        var seconds = t.getUTCSeconds();
        result = date > 1
            ? date + ' days ago'
            : hours > 0
                ? hours + ' hours ago'
                : minutes > 0
                    ? minutes + ' minutes ago'
                    : seconds + ' seconds ago';
        $(item).prev().text(result);
    });
};

//var removeDoubles = function() {
//    $('.clPhoto.newItem').each(function (index, item) {
//        var l;
//        if ($(item).hasClass('storyLike')) {
//            l = $('.clPhoto:not(.newItem).' + item.id + '.storyLike').length;
//        }
//        else if ($(item).hasClass('storyComment')) {
//            l = $('.clPhoto:not(.newItem).' + item.id + '.storyComment').length;
//        }
//        
//        if (l === 0) {
//            return;
//        } else {
//            $(item).remove();
//        }
//    });
//
//    $('.nine.column.row').each(function (index, item) {
//        if ($(item).children('.clPhoto').length == 0) {
//            $(item).remove();
//        }
//    });
//};

var loadAndSubscribe = function(data) {
    var lastUpdate = $(data).get($(data).length - 1);
    $('#newsGrid').prepend(data);
    $('#lastUpdate').val($(lastUpdate).val());
//    removeDoubles();
    subscribeLikes();
    loadPhotos();
    loadUsers();
    convertTimestamps();
    $('.lblLike').popup();
    $('.linkPhoto').popup();
};

var loadNews = function () {
    $('#gridLoader').show();
    $.ajax({
        url: "/Home/GetNewsPartial",
        data: { timestamp: $('#lastUpdate').val() === undefined ? 0 : $('#lastUpdate').val() }
    })
       .success(function (data) {
           if (data) {
               $('div.ui.divider').remove();
               $('.newItem').removeClass('newItem');
               $('.lblLike').popup('hide all');
               $('.linkPhoto').popup('hide all');
               loadAndSubscribe(data);
               $('#gridLoader').hide();
           }
       });
};

$('#newStories').click(function () {
    $('#newStories').hide();
    document.title = 'Instagram news';
    loadNews();
});


$(function () {
    $('#gridLoader').show();
    $.ajax({
            url: "/Home/GetNewsPartial",
            data: { timestamp: 0 }
        })
        .success(function(data) {
            if (data) {
                loadAndSubscribe(data);
                $('div.ui.divider').remove();
                $('#gridLoader').hide();
            }
        });
});
$(setInterval(checkNews, 60000));
