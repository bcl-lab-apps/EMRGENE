// (c) Microsoft. All rights reserved

function displayText(text) {
    var output = document.getElementById("output");
    //output.textContent = text;
}

function displayError(error) {
    var text = "ErrorCode = " + error.number.toString(16) + "\r\n";
    displayText(text + error.message);
}

function displayAlert(message) {
    new Windows.UI.Popups.MessageDialog(message).showAsync();
}

function displayContent(text) {
    var content = document.getElementById("itemList");
    content.innerHTML = text;
}
//---------------
//
// Simple tabular display
//
//---------------

function displayList(items, fullItem) {
    renderItemList(items, fullItem);

}

//bad construction, needs better design/modular design
//would be better to pass in function callback with the appropriate renders, this is to save time temporarily
function renderItemList(itemList, fullItem) {

    //empty item list
    if (itemList.length == 0) {
        return null;
    }

    var item = itemList[0]; //0 is the latest record

    //no item type
    if (!item.type) {
        return null;
    }

    //no item type name
    if (!item.type.name) {
        return null;
    }

    if (item.type.name.toString() == "Weight Measurement") {
        renderWeight(item);
    }

    if (item.type.name.toString() == "Cholesterol Measurement") {
        renderCholestrol(item);
    }

    if (item.type.name.toString() == "Blood Pressure Measurement") {
        renderBP(item);
    }

    if (item.type.name.toString() == "Allergy") {
        renderAllergy(itemList);
    }

    if (item.type.name.toString() == "Medication") {
        renderMedication(itemList);
    }

    if (item.type.name.toString() == "Condition") {
        renderCondition(itemList);
    }



    //return table;
}

//create a stringify method instead of using just toString because it needs to catch nulls
function stringify(thing) {
    if (thing) {
        return thing.toString();
    }

    else {
        return ""
    }
}

function renderWeight(item) {
    var itemName = stringify(item.type.name);
    var itemValue = stringify(item.value.value);
    var itemDate = stringify(item.when);
    var itemUnit = "kg";
    var itemInfo = itemValue + " " + itemUnit;
    var object = { name: itemName, date: itemDate, info1: itemInfo, info2: "", info3: "", info4: ""};
    LatestInfo.itemList.push(object);
}


function renderCholestrol(item) {
    var chol = stringify(item.type.name);
    var ldl = stringify(item.ldl);
    var hdl = stringify(item.hdl);
    var units = "mmol/L";
    var triglyceride = stringify(item.triglycerides);
    var when = stringify(item.when);
    var total = stringify(item.total);

    var itemInfo = "LDL: " + ldl + " " + units;
    var itemInfo2 = "HDL: " + hdl + " " + units;
    var itemInfo3 = "Triglyceride: " + triglyceride + " " + units;
    var itemInfo4 = "Total Cholestrol: " + total + units;
    var object = { name: chol, date: when, info1: itemInfo, info2: itemInfo2, info3: itemInfo3, info4: itemInfo4 };
    LatestInfo.itemList.push(object);
}

function renderBP(item) {
    var bp = stringify(item.type.name);
    var systolic = stringify(item.systolic);
    var diastolic = stringify(item.diastolic);
    var when = stringify(item.when);
    var pulse = stringify(item.pulse);
    var unit = " mmHg";
    var pulseunit = " beats per minute";
    var irregularHB = stringify(item.irregularHeartbeat);

    var itemInfo = "Systolic: " + systolic + unit;
    var itemInfo2 = "Diastolic: " + diastolic + unit;
    var itemInfo3 = "Pulse: " + pulse + pulseunit;
    var itemInfo4 = "Irregular Heartbeat: " + irregularHB;

    var object = { name: bp, date: when, info1: itemInfo, info2: itemInfo2, info3: itemInfo3, info4: itemInfo4 };
    LatestInfo.itemList.push(object);
}

function renderAllergy(itemList) {

    for (i = 0, count = itemList.length; i < count; ++i) {

        var item = itemList[i];

        var itemName = "Allergy: "+ stringify(item.name);
        
        var itemReaction = "Reaction: " + stringify(item.reaction);

        var itemDate = "First Observed: " + stringify(item.firstObserved);
    
        var objectA = { name: itemName, reaction: itemReaction, date: itemDate };

        AllergyInfo.itemList.push(objectA);

    }
}

function renderMedication(itemList) {

    for (i = 0, count = itemList.length; i < count; ++i) {

        var item = itemList[i];

        var itemName = "Medication: " + stringify(item.name);

        var itemStrength = "Strength: " + stringify(item.strength);

        var itemDose = "Dose: " + stringify(item.dose);

        var itemFrequency = "Frequency: " + stringify(item.frequency);

        var objectA = { name: itemName, dose: itemDose, strength: itemStrength, frequency: itemFrequency};

        MedicationInfo.itemList.push(objectA);

    }
}

function renderCondition(itemList) {
    for (i = 0, count = itemList.length; i < count; ++i) {

        var item = itemList[i];

        var itemName = "Condition: " + stringify(item.name);

        var itemStatus = "Status: " + stringify(item.status);

        var itemOnset = "On-set Date: " + stringify(item.onsetDate);

        var itemStopDate = "Stop Date: " + stringify(item.stopDate);

        var itemStopReason = "Stop Reason: " + stringify(item.stopReason);

        var objectA = { name: itemName, status: itemStatus, onset: itemOnset, stopDate: itemStopDate, stopReason: itemStopReason };

        ConditionInfo.itemList.push(objectA);

    }
}


function validateAndDisplayList(itemList, fullItem) {

    if (itemList == null) {
        displayAlert("Null Item List");
        return;
    }
    //
    // ensureAvailable will ensure that all PendingItems are also resolved
    // You can also use ensureAvailableAsync(startAt, count)
    //
    itemList.ensureAvailableAsync().then(
        function () {
            for (i = 0, count = itemList.length; i < count; ++i) {
                itemList[i].validate();
            }
            displayList(itemList, fullItem);
        },
        function (error) {
            displayError(error);
        },
        null
    );
}

function displaySerializableObjects(itemList) {

    var div = document.getElementById("itemList");

    var table = document.createElement("table");

    for (i = 0, count = itemList.length; i < count; ++i) {

        var item = itemList[i];
        var xml = item.serialize();

        var row = table.insertRow();
        var cell = row.insertCell();
        cell.innerText = xml;
    }

    if (div.firstChild != null) {
        div.removeChild(div.firstChild);
    }
    div.appendChild(table);
}

function displayVocabMatches(itemList) {

    var div = document.getElementById("itemList");

    var table = document.createElement("table");

    for (i = 0, count = itemList.length; i < count; ++i) {

        var item = itemList[i];

        var row = table.insertRow();

        var nameCell = row.insertCell();
        nameCell.innerText = item.displayText;

        var xmlCell = row.insertCell();
        xmlCell.innerText = item.serialize();
    }

    if (div.firstChild != null) {
        div.removeChild(div.firstChild);
    }
    div.appendChild(table);
}

function displayStrings(strings) {

    var div = document.getElementById("itemList");
    var table = document.createElement("table");

    for (i = 0, count = strings.length; i < count; ++i) {

        var item = strings[i];

        var row = table.insertRow();
        var cell = row.insertCell();
        cell.innerText = item;
    }

    if (div.firstChild != null) {
        div.removeChild(div.firstChild);
    }
    div.appendChild(table);
}

function ensureChildTable(div) {

    var table = div.firstChild;

    if (table != null) {
        if (table.tagName == "TABLE") {
            return table;
        }
        div.removeChild(table);
    }

    table = document.createElement("table");
    div.appendChild(table);

    return table;
}

function beginLog() {
    var div = document.getElementById("itemList");
    if (div.firstChild != null) {
        div.removeChild(div.firstChild);
    }

    return div;
}

function writeLine(parentDiv, text) {
    var table = ensureChildTable(parentDiv);
    var row = table.insertRow();
    var cell = row.insertCell();
    cell.innerText = text;
}

//----------------------------
//
// Application Startup
//
//----------------------------
var g_hvApp = createHealthVaultApp();
startApp();

function createHealthVaultApp() {
    var app = new HealthVault.Foundation.HealthVaultApp(new HealthVault.Foundation.HealthVaultAppSettings("04bd13fd-875c-41e3-bd11-4cae8c88cf37"));
    app.appInfo.instanceName = "DB EMR";
    app.debugMode = true;

    return app;
}

function getCurrentRecord() {
    return g_hvApp.userInfo.authorizedRecords[0];
}

function getCurrentRecordStore() {
    return g_hvApp.localVault.recordStores.getStoreForRecord(getCurrentRecord());
}


function clearLists() {
    AllergyInfo.itemList.forEach(function (value, index, array) {
        AllergyInfo.itemList.pop();
    });
    
    LatestInfo.itemList.forEach(function (value, index, array) {
        LatestInfo.itemList.pop();
    });

    MedicationInfo.itemList.forEach(function (value, index, array) {
        MedicationInfo.itemList.pop();
    });

    ConditionInfo.itemList.forEach(function (value, index, array) {
        ConditionInfo.itemList.pop();
    });
};

function startApp() {
    g_hvApp.startAsync().then(
        function () {
            renderUserInfo();
            //clears the list
            clearLists();

            //the reconfirmation of authentication is for when the user is not logged in
            g_hvApp.isAuthorizedOnServerAsync().then(function (result) {

                if (result) {
                    if (g_hvApp.userInfo) {
                        document.getElementById("loginbutton").innerHTML = "Logout";
                        getAllergy();
                        getBloodPressure();
                        getCholestrol();
                        getCondition();
                        getMedication();
                        getWeight();
                    }
                }

            });

        },
        displayError,
        null);
}

function restartApp() {
    g_hvApp.resetAsync().then(startApp());
    document.getElementById("loginbutton").innerHTML = "Login";
}

function authMore() {
    g_hvApp.authorizeAdditionalRecordsAsync().then(
        function () {
            displayUser();
        },
        displayError,
        null);
}

function isAuthOnServer() {
    g_hvApp.isAuthorizedOnServerAsync().then(
        function(result) {
            if (result)
                displayText("Yes");
            else
                displayText("No");
        },
        null,
        null
    );
}

function isHVAuthenticated() {
    return g_hvApp.isAuthorizedOnServerAsync()
}

function removeAppRecordAuthOnServer() {
    var promises = [];

    for (var i = 0; i < g_hvApp.userInfo.authorizedRecords.length; i++) {
        promises.push(g_hvApp.userInfo.authorizedRecords[i].removeApplicationRecordAuthorizationAsync());
    }

    if (promises.length > 0) {
        WinJS.Promise.join(promises).then(function() {
            return g_hvApp.resetAsync();
        }).then(function() {
            displayText("start/restart app");
        });
    }
}

//----------------------------
//
// Serialization Tests
//
//----------------------------
function displayUser() {
    if (!g_hvApp.hasUserInfo) {
        displayText("No user");
        return;
    }

    var userInfo = g_hvApp.userInfo;
    var xml = userInfo.serialize();
    displayText(xml);
}

function testItemFilter() {
    var filter = new HealthVault.Types.ItemFilter();

    filter.effectiveDateMax = new HealthVault.Types.DateTimeValue("12/3/2010");
    filter.updatedDateMin = new HealthVault.Types.DateTimeValue("5/6/1993");

    displayText(filter.serialize());
}

function testCodableValue() {
    var code = new HealthVault.Types.CodedValue("Snomed", "12346");

    var codableValue = new HealthVault.Types.CodableValue("Lipitor", code);

    codableValue.codes.addIfDoesNotExist(code);
    code = new HealthVault.Types.CodedValue("Snomed", "abcd");
    codableValue.codes.addIfDoesNotExist(code);

    var xml = codableValue.serialize();
    displayText(xml);

    codableValue = HealthVault.Types.CodableValue.deserialize(xml);
    displayText(codableValue.serialize());

}

function testMedication() {
    var med = makeMedication();

    var xml = med.serialize();
    med = HealthVault.ItemTypes.Medication.deserialize(xml);

    xml = med.serialize();
    displayText(xml);
}

function testCondition() {
    var cond = makeCondition();

    var xml = cond.serialize();
    cond = HealthVault.ItemTypes.Condition.deserialize(xml);

    xml = cond.serialize();
    displayText(xml);
}

function testItemQuery() {

    var query = new HealthVault.Types.ItemQuery();
    query.name = "Foo";
    query.view.typeVersions.append("33");
    query.itemIDs = new Array
    (
        HealthVault.ItemTypes.Medication.typeID,
        HealthVault.ItemTypes.Weight.typeID,
        HealthVault.ItemTypes.Height.typeID
    );
    query.keys = new Array
    (
        new HealthVault.Types.ItemKey("1234", "1.1")
    );

    query.maxResults = new HealthVault.Types.NonNegativeInt(1000);
    query.maxFullItems = new HealthVault.Types.NonNegativeInt(100);
    var xml = query.serialize();
    displayText(xml);
}

//----------------------------
//
// Cache
//
//----------------------------
function testLRUCache() {

    try {
        var maxItems = 10;
        var cache = new HealthVault.Store.ObjectCache(maxItems, false);
        var maxTestItems = 15;

        var i = 0;
        for (; i < maxItems; ++i) {
            var key = i.toString();
            var value = key + "__Value";
            cache.put(key, value);
        }

        for (j = 0; j < i / 2; ++j) {
            var value = cache.get(j.toString());
        }

        for (; i < maxTestItems; ++i) {
            var key = i.toString();
            var value = key + "__Value";
            cache.put(key, value);
        }

        displayStrings(cache.getAllKeys());
    }
    catch (ex) {
        displayError(ex);
    }
}

function setLocalStoreMemCacheSize(cacheSize) {

    g_hvApp.localVault.recordStores.maxCachedItems = cacheSize;
}

//----------------------------
//
// Local Storage
//
//----------------------------

function deleteRecordStore() {
    var record = getCurrentRecord();
    try {
        var log = beginLog();
        g_hvApp.localVault.recordStores.removeStoreForRecord(record);
        writeLine(log, "Done");

    } catch (e) {
        displayError(e);
    }
}

function putItemsInLocalStore() {

    var record = getCurrentRecord();
    var store = g_hvApp.localVault.recordStores.getStoreForRecord(record);

    var maxItems = 100;
    var weight;
    var log = beginLog();
    for (i = 0; i < maxItems; ++i) {
        weight = makeWeight();
        weight.key = HealthVault.Types.ItemKey.newKey();
        writeLine(log, weight.serialize());
        store.data.local.putAsync(weight).then(
           null,
           displayError,
            null
        );
    }
}

function getItemsFromLocalStore() {
    try {
        var record = getCurrentRecord();
        var store = g_hvApp.localVault.recordStores.getStoreForRecord(record);
        var log = beginLog();
        writeLine(log, "Starting");

        store.data.local.getItemIDsAsync().then(
            function (ids) {
                for (i = 0; i < ids.length; ++i) {
                    store.data.local.getByIDAsync(ids[i]).then(
                        function (item) {
                            writeLine(log, item.serialize());
                        },
                        displayError,
                        null
                    );
                }
            }
        );
    }
    catch (ex) {
        displayError(ex);
    }
}

//----------------------------
//
// Synchronized Store
//
//----------------------------

function renderKeys(keys) {
    var lines = new Array();
    if (keys != null) {
        for (i = 0; i < keys.length; ++i) {
            lines.push(keys[i].serialize());
        }
    }

    return lines.join("\r\n");
}

function displayKeys(keys) {
    var text = "Pending Keys Downloaded In Background:\r\n";
    text = text + renderKeys(keys);
    displayText(text);
}

function displayKeysNotFound(keys) {
    var text = "FAILED TO DOWNLOAD Keys In Background:\r\n";
    text = text + renderKeys(keys);
    displayText(text);
}

function displayPendingGetResult(result) {
    displayKeys(result.keysFound);
}

function logItems(log, keys, items) {
    for (i = 0; i < items.length; ++i) {
        var item = items[i];
        if (item != null) // CAN BE NULL
        {
            writeLine(log, item.serialize());
        }
        else {
            writeLine(log, "****LOADING***** " + keys[i].serialize());
        }
    }
}

function displayItemsWithKeys(log, record, keys, wait) {

    var store = g_hvApp.localVault.recordStores.getStoreForRecord(record);
    //
    // This waits until all pending items have arrived
    //
    if (wait == true) {
        store.data.getAsync(keys).then(
            function (items) {
                logItems(log, keys, items);
            },
            displayError
        );
    }
    else {
        // 
        // This will return local items immediately, and notify us when pending items are available
        //
        store.data.getAsync(keys, function (sender, result) { displayPendingGetResult(result); }).then(
            function (items) {
                logItems(log, keys, items);
            },
            displayError
        );
    }
}

function testSynchronizedStoreFor(typeID, awaitAll) {

    if (arguments.length < 2) {
        awaitAll = false;
    }

    displayText("");
    var log = beginLog();
    var record = getCurrentRecord();
    var filter = HealthVault.Types.ItemFilter.filterForType(typeID);
    //
    // Currently Fetch all keys from HV... but read any locally cached items..
    // Tomorrow - start storing the keys also on disk
    //
    record.getKeysAsync([filter]).then(

        function (keys) {
            if (keys.size > 0) {
                displayItemsWithKeys(log, record, keys, awaitAll);
            }
            else {
                displayText("No items found");
            }
        },
        displayError

    );
}

//----------------------------
//
// Synchronized View
//
//----------------------------

function makeViewNameFor(typeID) {
    return typeID + "_Full";
}

/// <param
function subscribeViewEvents(view) {
    view.addEventListener("error", displayError);
    view.addEventListener("itemsavailable", displayKeys);
    view.addEventListener("itemsnotfound", displayKeysNotFound);
}

function saveSyncView(view, store) {

    store.putViewAsync(view).then(
        function () {
            displayText("Saved View");
            displayText(view.data.serialize());
        },
        displayError
    );
}

function deleteSyncViewFor(typeID) {
    getCurrentRecordStore().deleteViewAsync(makeViewNameFor(typeID)).then(
        function () {
            displayText("Sync view deleted");
        });
}

function ensureSyncViewFor(typeID, store) {

    var viewName = makeViewNameFor(typeID);

    store.getViewAsync(viewName).then(
        function (view) {
            if (view == null) {

                // NO saved view. Create a new one
                view = store.createView(viewName, HealthVault.Types.ItemQuery.queryForTypeID(typeID));
                synchronizeSyncView(view, store);
            }
        }
    );
}

function synchronizeSyncView(view, store) {

    displayText("Synchronizing View");
    view.synchronizeAsync()
        .then(function () {
            displayText("Synchronized. Saving.");
            return store.putViewAsync(view)
        })
        .then(function () {  // Render saved view
            renderSynchronizedView(view);
            //renderSynchronizedViewSequential(view);
            //renderSynchronizedViewBlocking(beginLog(), view);
        }, function (error) {
            displayError(error);
        });
}

function renderSynchronizedViewBlocking(log, view) {

    for (i = 0; i < view.keyCount; ++i) {
        var item = view.ensureItemAvailableAndGetSync(i);  // BLOCKING CALL
        //var item = view.getItemSync(i); // BLOCKING CALL
        if (item == null) {
            writeLine(log, "NOT FOUND");
        }
        else {
            writeLine(log, item.serialize());
        }
    }
}

function renderSynchronizedViewChunky(log, view) {

    var chunkSize = view.keyCount;
    view.ensureItemsAvailableAndGetAsync(0, chunkSize).then(
        function (items) {
            var log = beginLog();
            for (i = 0; i < items.length; ++i) {
                var item = items[i];
                if (item == null) {
                    writeLine(log, "NOT FOUND");
                }
                else {
                    writeLine(log, item.serialize());
                }
            }
        },
        displayError
    );
}

function ensureGetItemCompleted(log, view, item, keyIndex) {

    if (item == null) {
        writeLine(log, "LOADING...");
    }
    else {
        writeLine(log, item.serialize());
    }

    if (keyIndex < view.keyCount - 1) {
        return renderSynchronizedViewItemSequential(log, view, keyIndex + 1);
    }
}

//
// Sequential rendering - but ASYNCHRONOUSLY
//
function renderSynchronizedViewItemSequential(log, view, keyIndex) {

    if (arguments.length < 3) {
        keyIndex = 0;
    }

    if (keyIndex >= view.keyCount) {
        return;
    }
    //
    // Run IN ORDER... but NOT in parallel
    //
    return view.ensureItemAvailableAndGetAsync(keyIndex)
        .then(function (item) {
            ensureGetItemCompleted(log, view, item, keyIndex);
        }, displayError
        );
}

function renderSynchronizedViewSequential(view) {

    var log = beginLog();
    renderSynchronizedViewItemSequential(log, view, 0);
}

function renderSynchronizedView(view) {

    subscribeViewEvents(view);

    var keyCount = view.keyCount;
    var promises = new Array();
    //
    // Collect all pending promises
    //
    for (i = 0; i < keyCount; ++i) {
        promises[i] = view.getItemAsync(i);
    }
    //
    // Now run them in PARALLEL...but return results in order
    //
    WinJS.Promise.thenEach(promises,
        function (item) {
            if (item == null) {
                return "LOADING";
            }
            else {
                return item.serialize();
            }
        }
    )
    .done(
        function (results) {
            displayStrings(results);
        }
    );
}

function synchronizeViewFor(typeID) {

    var viewName = makeViewNameFor(typeID);
    var store = getCurrentRecordStore();

    store.getViewAsync(viewName)
        .then(function (view) {
            if (view != null) {
                synchronizeSyncView(view, store);
            }
            else {
                displayText("No view to sync");
            }
        });
}

function testSynchronizedViewFor(typeID) {

    displayText("");

    var viewName = makeViewNameFor(typeID);
    var store = getCurrentRecordStore();
    var maxAgeSeconds = 60 * 60;

    store.getViewAsync(viewName).then(
        function (view) {
            if (view == null) {
                ensureSyncViewFor(typeID, store);
                return;
            }

            if (view.isStale(maxAgeSeconds)) {
                synchronizeSyncView(view, store);
                return;
            }

            displayText("View is FRESH");
            renderSynchronizedView(view);
        },
        displayError
    );
}

function testRenderSynchronizedViewBlockingFor(typeID) {

    displayText("");

    var viewName = makeViewNameFor(typeID);
    var store = getCurrentRecordStore();
    store.getViewAsync(viewName)
        .then(function (view) {
            if (view != null) {
                renderSynchronizedViewBlocking(beginLog(), view);
            }
            else {
                displayText("No view has been created");
            }
        });
}

function testRenderSynchronizedChunkyFor(typeID) {

    displayText("");

    var viewName = makeViewNameFor(typeID);
    var store = getCurrentRecordStore();
    store.getViewAsync(viewName)
        .then(function (view) {
            if (view != null) {
                renderSynchronizedViewChunky(beginLog(), view);
            }
            else {
                displayText("No view has been created");
            }
        });
}

//----------------------------
//
// Vocab
//
//----------------------------

function testVocab() {

    var vocabIDs = new Array(
        HealthVault.ItemTypes.Medication.vocabForDoseUnits(),
        HealthVault.ItemTypes.Medication.vocabForStrengthUnits()
    );

    g_hvApp.vocabs.getAsync(vocabIDs).then(
        function (vocabs) {
            var xml = "";
            for (i = 0; i < vocabs.length; ++i) {
                var vocab = vocabs[i];
                xml = xml + vocab.serialize();
            }
            displayText(xml);
        },
        function (error) {
            if (error.number == HealthVault.Foundation.ServerErrorNumber.vocabNotFound) {
                displayText("Vocabulary not found");
            }
            else {
                displayError(error)
            }
        },
        null
    );
}

function testVocabSearch() {
    var rxNorm = HealthVault.ItemTypes.Medication.vocabForName();
    var text = Array.randomItemFrom("Lipitor", "Cialis", "Paxal", "Ibuprofen", "Imitrex", "Wellbutrin");

    g_hvApp.vocabs.searchAsync(rxNorm, text).then(
        function (result) {
            if (result.hasItems) {
                displayVocabMatches(result.items);
            }
        },
        displayError,
        null
    );
}

function testVocabStore() {

    var vocabIDs = new Array(
        HealthVault.ItemTypes.Medication.vocabForDoseUnits(),
        HealthVault.ItemTypes.Medication.vocabForStrengthUnits()
    );

    var maxAgeSeconds = 24 * 3600;  // 1 day...in practice, should be more like 2-3 months
    //
    // If the vocab is not available, OR stale, will trigger a download in the background
    // However the UI should NOT freeze while the vocab downloads. In fact, the download could fail.
    // The UI must keep working even if the required vocab is NOT available. 
    // Therefore, you should typically call ensureVocabs opportunistically, and EARLY
    //
    g_hvApp.localVault.vocabStore.ensureVocabsAsync(vocabIDs, maxAgeSeconds);
    //
    // This may return null if the vocab has not arrived yet
    //
    g_hvApp.localVault.vocabStore.getAsync(vocabIDs[0]).then
    (
        function (vocab) {
            displayText(vocab.serialize());
        }
    );
}
//----------------------------
//
// WIRE tests
//
//----------------------------
function testGetThings() {

    var query = new HealthVault.Types.ItemQuery();
    query.name = "GetThingsMultiple";

    var filter = new HealthVault.Types.ItemFilter(new Array(
        HealthVault.ItemTypes.Condition.typeID,
        HealthVault.ItemTypes.File.typeID,
        HealthVault.ItemTypes.Medication.typeID,
        HealthVault.ItemTypes.Procedure.typeID,
        HealthVault.ItemTypes.Weight.typeID,
        HealthVault.ItemTypes.Height.typeID
    ));
    query.filters.append(filter);
    query.maxResults = new HealthVault.Types.NonNegativeInt(100);
    //query.maxFullItems = new HealthVault.Types.NonNegativeInt(1);
    testQuery(query, true);
}

function testQuery(query, fullItem) {

    if (arguments.length < 2) {
        fullItem = false;
    }

    //query.filters[0].updatedDateMin = new HealthVault.Types.DateTime("8/17/2012");

    displayText(query.serialize());
    //var xml = query.serialize();
    //var recordItem = HealthVault.Types.RecordItem.deserialize(xml);



    var record = getCurrentRecord();

    if (record) {
        record.getAsync(query).then(
       function (itemList) {
           validateAndDisplayList(itemList, fullItem);
       },
       displayError,
       null
        );
    }

   


}

//
// Forces all items to be resolved via PendingKeys...
//
function testQueryWithPendingGet(query, fullItem) {

    if (arguments.length < 2) {
        fullItem = false;
    }

    query.maxFullItems = new HealthVault.Types.NonNegativeInt(0);  // Don't inline any things...
    displayText(query.serialize());

    var record = getCurrentRecord();

    record.getAsync(query).then(
        function (itemList) {
            validateAndDisplayList(itemList, fullItem);
        },
        displayError,
        null
    );
}

function testPutItem(item) {

    var record = getCurrentRecord();
    record.putAsync(item).then(
        function (key) {
            displayText(key.serialize());
        },
        displayError,
        null
    );
}

function testRemoveThings() {
    var record = getCurrentRecord();

    var query = HealthVault.ItemTypes.Weight.queryFor();
    record.getItemsAsync(query).then(
        function (result) {
            var key = result.items[0].key;
            record.removeAsync(key);
        },
        displayError,
        null
    );
}

function testOpenFile() {
    var record = getCurrentRecord();

    var query = HealthVault.ItemTypes.File.queryFor();
    record.getAsync(query).then(
        function (itemList) {
            var firstFile = itemList[0];
            firstFile.display(record);
        },
        displayError,
        null
    );
}

function downloadFile(record, file) {

    var recordStore = g_hvApp.localVault.recordStores.getStoreForRecord(record);
    var blobStore = recordStore.blobs;
    blobStore.openWriteStreamAsync(file.key.id).then(
        function (stream) {
            file.downloadAsync(record, stream).then(
                function (success) {
                    displayAlert(success);
                },
                displayError,
                null
            )
        },
        displayError,
        null
    );
}

function testSaveFile() {

    var record = getCurrentRecord();

    var query = HealthVault.ItemTypes.File.queryFor();
    record.getAsync(query).then(
        function (itemList) {
            var firstFile = itemList[0];
            downloadFile(record, firstFile);
        },
        displayError,
        null
    );
}

function testUploadFile() {

    var record = getCurrentRecord();
    var Pickers = Windows.Storage.Pickers;
    var picker = new Pickers.FileOpenPicker();
    picker.viewMode = Pickers.PickerViewMode.list;
    picker.fileTypeFilter.replaceAll(["*"]);

    picker.pickSingleFileAsync().then
    (
        function (storageFile) {
            var file = new HealthVault.ItemTypes.File();
            file.uploadFileAsync(record, storageFile).then(
                null,
                displayError,
                null
            );
        }
    )
}

function testGetPersonalImage() {

    var record = getCurrentRecord();

    record.getAsync(HealthVault.ItemTypes.PersonalImage.queryFor()).then(
        function (items) {
            /// <param name="items" type="HealthVault.ItemTypes.ItemDataTypedList" />
            if (items.size > 0) {
                refreshPersonalImage(record, items[0]);
            }
        });
}

function displayImage(image) {

    var imageUrl = URL.createObjectURL(image, { oneTimeOnly: true });

    var imageElement = document.getElementById("recordImage");
    if (!imageElement) {
        var imageDiv = document.getElementById("recordImageCont");
        imageElement = document.createElement("img");
        imageElement.id = "recordImage";
        imageElement.src = imageUrl;
        imageDiv.appendChild(imageElement);
    }
    else {
        imageElement.src = imageUrl;
    }
}

function refreshPersonalImage(record, personalImage) {
    /// <param name="record" type="HealthVault.Foundation.IRecord" />
    /// <param name="personalImage" type="HealthVault.ItemTypes.PersonalImage" />

    var imageStreamName = "personalImage";
    var stream;
    var store = getCurrentRecordStore();
    store.blobs.openWriteStreamAsync(imageStreamName)
        .then(function (writeStream) {
            /// <param name="writeStream" type="System.IO.Stream" />
            stream = writeStream;
            return personalImage.downloadAsync(record, writeStream);
        })
        .then(function (complete) {
            stream.close();
            if (complete) {
                return store.blobs.openContentStreamAsync(imageStreamName);
            }
            else {
                displayAlert("personal image failed to download");
            }
        }, function (error) {
            displayAlert(error);
            stream.close();
        })
        .then(function (imageStream) {
            displayImage(imageStream);

            displayAlert("personal image refreshed");
        });
}

function openPersonalImage(record, personalImage) {
    /// <param name="record" type="HealthVault.Foundation.IRecord" />
    /// <param name="personalImage" type="HealthVault.ItemTypes.PersonalImage" />

    var imageStreamName = "personalImage.jpg";
    var stream;
    var store = getCurrentRecordStore();
    store.blobs.openWriteStreamAsync(imageStreamName)
        .then(function (writeStream) {
            /// <param name="writeStream" type="System.IO.Stream" />
            stream = writeStream;
            return personalImage.downloadAsync(record, writeStream);
        })
        .then(function (complete) {
            stream.close();
            if (complete) {
                return store.blobs.getStorageFileAsync(imageStreamName);
            }
            else {
                displayAlert("personal image failed to download");
            }
        }, function (error) {
            displayAlert(error);
            stream.close();
        }).then(function (file) {
            if (file) {
                var imageElement = document.getElementById("recordImage");
                if (!imageElement) {
                    var imageDiv = document.getElementById("recordImageCont");
                    imageElement = document.createElement("img");
                    imageElement.id = "recordImage";
                    imageElement.src = file.path;
                    imageDiv.appendChild(imageElement);
                }
                else {
                    imageElement.src = imageUrl;
                }

                // Windows.System.Launcher.launchFileAsync(file);
            }
        });
}

function testMakePersonalImage() {

    var record = getCurrentRecord();
    var picker = new Windows.Storage.Pickers.FileOpenPicker();

    picker.viewMode = Windows.Storage.Pickers.PickerViewMode.list;
    picker.fileTypeFilter.replaceAll([".png", ".jpg", ".jpeg"]);

    picker.pickSingleFileAsync().then(
        function(storageFile) {
            if (!storageFile) {
                return;
            }

            record.getAsync(HealthVault.ItemTypes.PersonalImage.queryFor()).then(
                function(items) {
                    var personalImage;
                    if (items.size > 0) {
                        personalImage = items[0];
                    } else {
                        personalImage = new HealthVault.ItemTypes.PersonalImage();
                    }

                    personalImage.uploadFileAsync(record, storageFile).then(
                        function() {
                            displayAlert("personal image uploaded");
                        },
                        displayError,
                        null);
                }
            );
        }
    );
}

// Basic is a singleton so you cannot just add a new one if one already exists.
function testMakeBasic() {
    var record = getCurrentRecord();
    record.getAsync(HealthVault.ItemTypes.BasicV2.queryFor()).then(function(items) {
        var basic;
        if (items && items.length > 0) {
            basic = items[0];
        } else {
            basic = new HealthVault.ItemTypes.BasicV2();
        }

        basic.gender = 'f';
        basic.birthYear = new HealthVault.Types.Year(1979);
        basic.city = 'Hollywood';
        basic.country = new HealthVault.Types.CodableValue('United States');
        basic.firstDayOfWeek = new HealthVault.Types.DayOfWeek(1);
        basic.postalCode = '90210';
        basic.state = new HealthVault.Types.CodableValue('California');
        basic.languages = [new HealthVault.Types.Language(new HealthVault.Types.CodableValue('Piglatin'), true)];

        return testPutItem(basic);
    });
}

// Personal is a singleton so you cannot just add a new one if one already exists.
function testMakePersonal() {
    var record = getCurrentRecord();
    record.getAsync(HealthVault.ItemTypes.Personal.queryFor()).then(function (items) {
        var personal;
        if (items && items.length > 0) {
            personal = items[0];
        } else {
            personal = new HealthVault.ItemTypes.Personal();
        }

        personal.name = new HealthVault.Types.Name("First", "Middle", "Last");
        var birthDate = new HealthVault.Types.StructuredDateTime();
        birthDate.date = new HealthVault.Types.Date("1982", "1", "1");
        personal.birthDate = birthDate;
        personal.bloodType = new HealthVault.Types.CodableValue("B+");
        personal.ethnicity = new HealthVault.Types.CodableValue("Indian");
        personal.nationalIdentifier = "123-45-6789";
        personal.maritalStatus = new HealthVault.Types.CodableValue("Single");
        personal.employmentStatus = "Employed";
        personal.isDeceased = new HealthVault.Types.BooleanValue(false);
        var approxDateOfDeath = new HealthVault.Types.ApproxDateTime();
        approxDateOfDeath.description = "Never";
        personal.dateOfDeath = approxDateOfDeath;
        personal.religion = new HealthVault.Types.CodableValue("Buddhism");
        personal.isVeteran = new HealthVault.Types.BooleanValue(true);
        personal.educationLevel = new HealthVault.Types.CodableValue("phd");
        personal.isDisabled = new HealthVault.Types.BooleanValue(false);
        personal.organDonor = "false";

        return testPutItem(personal);
    });
}

// This shows that given raw xml we can call a put things
// The usage is given raw xml where you do not know the type or anything about it
// you can still call put things on it to put it into HV.
function testPutThingsRaw() {
    var allergy = new HealthVault.ItemTypes.Allergy("Honey");
    var xml = allergy.item.serialize();
    var record = getCurrentRecord();
    record.putRawAsync(xml).then(function(itemKeys) {
        if (itemKeys != null) {
            var itemKeysText = '';
            for(var i =0; i < itemKeys.length; i++) {
                itemKeysText += itemKeys[i].serialize();
            }
            displayText(itemKeysText);
        } else {
            displayText("Error must have happened, no keys returned");
        }
    });
}

// This shows that given some raw XML where we do not know the exact type
// We can parse it out as a record item to get other base attributes, specifically
// a type ID to get some partial information on what is being passed in.
function testParseRaw() {
    var allergy = new HealthVault.ItemTypes.Allergy("Honey");
    var xml = allergy.item.serialize();
    xml = xml.replace(/allergy/g, 'unknown-type');
    xml = xml.replace('52bf9104-2c5e-4f1f-a66d-552ebcc53df7', 'custom-id');

    var recordItem = HealthVault.Types.RecordItem.deserialize(xml);
    displayText(recordItem.type.id);
}

function testGetThingTypes() {
    var parameters = new HealthVault.Types.ThingTypeGetParams();
    HealthVault.ItemTypes.ItemTypeManager.getItemTypeDefinitions(g_hvApp, parameters).then(
        function (results) {
            var text = '';
            for (var i = 0; i < results.length; i++) {
                var result = results[i];
                text += result.typeId + ' - ' + result.name + '<br/>'
            }
            displayContent(text);
        });
}

//userInfo is different because is it provided upon startup
function renderUserInfo() {
    var info = g_hvApp.userInfo;

    UsernameInfo.itemList.pop();

    if (!g_hvApp.userInfo) {
        var noname = {name: "Not Logged In"};
        UsernameInfo.itemList.push(noname);
    }

    else {
        var username = { name: String(info.name) };
        UsernameInfo.itemList.push(username);
    }

}

//functions to get information upon startup

function getAllergy() {
    testQuery(HealthVault.ItemTypes.Allergy.queryFor());
}

function getWeight() {
    testQuery(HealthVault.ItemTypes.Weight.queryFor());
}

function getBloodPressure() {
    testQuery(HealthVault.ItemTypes.BloodPressure.queryFor());
}

function getCholestrol() {
    testQuery(HealthVault.ItemTypes.Cholesterol.queryFor());
}

function getMedication() {
    testQuery(HealthVault.ItemTypes.Medication.queryFor());
}

function getCondition() {
    testQuery(HealthVault.ItemTypes.Condition.queryFor());
}