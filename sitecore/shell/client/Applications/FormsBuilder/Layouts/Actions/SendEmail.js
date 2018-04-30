(function (speak) {
    var parentApp = window.parent.Sitecore.Speak.app.findApplication('EditActionSubAppRenderer');
    var designBoardApp = window.parent.Sitecore.Speak.app.findComponent('FormDesignBoard');

    //Gets the fields from the form
    var getFields = function () {
        var fields = designBoardApp.getFieldsData();

        return _.reduce(
            fields,
            function (memo, item) {
                if (item && item.model && item.model.hasOwnProperty("value")) {
                    memo.push({
                        itemId: item.itemId,
                        name: item.model.name
                    });
                }

                return memo;
            },
            [
                {
                    itemId: '',
                    name: ''
                }
            ],
            this
        );
    };

    speak.pageCode(["underscore"],
        function (_) {
            return {
                initialized: function () {
                    this.on({
                        "loaded": this.loadDone
                    }, this);

                    this.Fields = getFields();

                    if (parentApp) {
                        //Set the window header title and subtitle (from the HeaderTitle and HeaderSubtitle fields)
                        parentApp.loadDone(this, this.HeaderTitle.Text, this.HeaderSubtitle.Text);
                        parentApp.setSelectability(this, true);

                        var tokenString = '';
                        this.Fields.forEach(function (field) {
                            if (field.name != '') {
                                tokenString += ' [' + field.name + ']';
                            }
                        });

                        this.ValidTokens.Text = 'Valid tokens:' + tokenString;
                    }
                },

                loadDone: function (parameters) {
                    this.Parameters = parameters || {};
                    this.EmailForm.BindingTarget = this.Parameters;
                },

                getData: function () {
                    var formData = this.EmailForm.getFormData();
                    var keys = _.keys(formData);

                    keys.forEach(function (propKey) {
                        if (formData[propKey] == null || formData[propKey].length == 0) {
                            if (this.Parameters.hasOwnProperty(propKey)) {
                                delete this.Parameters[propKey];
                            }
                        } else {
                            this.Parameters[propKey] = formData[propKey];
                        }
                    }.bind(this));

                    return this.Parameters;
                }
            };
        }
    );
})(Sitecore.Speak);