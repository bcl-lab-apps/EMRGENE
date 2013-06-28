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

     var latestInfo =[ { name: "Weight", value: "", unit: "", info: "", date: "" },
   { name: "Blood Pressure", value: "", unit: "", info: "", date: "" },
    { name: "Cholestrol", value: "", unit: "", info: "", date: "" },
    { name: "Medication", value: "", unit: "", info: "", date: "" },
    { name: "Allergy", value: "", unit: "", info: "", date: "" },
    { name: "Condition", value: "", unit: "", info: "", date: "" }]

     //latestInfo = [];

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

    //Family History
    var fhList = new WinJS.Binding.List(familyHistory);

    var fhPM =
        {
            itemList: fhList
        };

    WinJS.Namespace.define("FHInfo", fhPM);

})();

