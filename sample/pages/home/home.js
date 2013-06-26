// (c) Microsoft. All rights reserved

(function () {
    "use strict";

    WinJS.UI.Pages.define("/pages/home/home.html", {
        // This function is called whenever a user navigates to this page. It
        // populates the page elements with the app's data.
        ready: function (element, options) {
            WinJS.UI.processAll();
        }
    });
})();

(function () {
    "use strict";

    var dataArray = [
    { "name": "Weight" },
    { "name": "Blood Pressure" },
    { "name": "Cholestrol" },
    { "name": "Medication" },
    { "name": "Condition"}
    ];

    var dataList = new WinJS.Binding.List(dataArray);

    // Create a namespace to make the data publicly
    // accessible. 
    var publicMembers =
        {
            itemList: dataList
        };
    WinJS.Namespace.define("DataHeaders", publicMembers);

})();

