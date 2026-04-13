function highlightFinalAmount(executionContext) {
    var formContext = executionContext.getFormContext();
    // Get the control (the visual part) of the field
    var amountControl = formContext.getControl("apollo_finalamount");

    if (amountControl) {
        // Note: This is unsupported by Microsoft but commonly used for UI emphasis
        var element = document.getElementById("apollo_finalamount_d");
        if (element) {
            element.style.border = "2px solid #0078d4"; // Apollo Blue border
            element.style.backgroundColor = "#f3f2f1"; // Light gray background
            element.style.padding = "10px";
        }
    }
}