(function() {

    function MelwaysResultViewModel() {
        var self = this;

        self.addressText = ko.observable();
        self.melwaysResult = ko.observableArray();

        self.addressSearch = function () {
            
            $.ajax({
                type: "POST",
                url: "/address",
                data: '{ "address": ' + ko.toJSON(self.addressText) + ' }',
                contentType: "application/json",
                success: function(result) { self.melwaysResult(JSON.parse(result));
                },
            });
        }
    }
    ko.applyBindings(new MelwaysResultViewModel());
})();