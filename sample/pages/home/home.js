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

    var allergyArray = [];
    var medication = [];
    var condition = [];
    var familyHistory = [];

    var latestInfo = [];

    var usernameInfo = [];

    //Lastest Information such as BP and Cholestrol
    var dataList = new WinJS.Binding.List(latestInfo);

    // Create a namespace to make the data publicly
    // accessible. 
    var publicMembers =
        {
            itemList: dataList
        };
    WinJS.Namespace.define("LatestInfo", publicMembers);

    //Allergy
    var allergyList = new WinJS.Binding.List(allergyArray);

    var allergyPM =
        {
            itemList: allergyList
        };
    WinJS.Namespace.define("AllergyInfo", allergyPM);
    //Medication
    var medList = new WinJS.Binding.List(medication);

    var medicationPM =
        {
            itemList: medList
        };
    WinJS.Namespace.define("MedicationInfo", medicationPM);
    //Conditions

    var conditionList = new WinJS.Binding.List(condition);

    var conditionPM =
        {
            itemList: conditionList
        };

    WinJS.Namespace.define("ConditionInfo", conditionPM);

    //UserName
    var unList = new WinJS.Binding.List(usernameInfo);

    var unPM =
        {
            itemList: unList
        };

    WinJS.Namespace.define("UsernameInfo", unPM);

})();

