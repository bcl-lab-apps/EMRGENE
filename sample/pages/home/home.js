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
    { name: "Weight", value: "", unit: "", info: "", date: "" },
    { name: "Blood Pressure", value: "", unit: "", info: "", date: "" },
    { name: "Cholestrol", value: "", unit: "", info: "", date: "" },
    { name: "Medication", value: "", unit: "", info: "", date: "" },
    { name: "Allergy", value: "", unit: "", info: "", date: "" },
    { name: "Condition", value: "", unit: "", info: "", date: "" }
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

