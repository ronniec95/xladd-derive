"use strict";

function getStyle(className) {
    var cssText = "";
    var classes = document.styleSheets[0].rules || document.styleSheets[0].cssRules;
    for (var x = 0; x < classes.length; x++) {
        if (classes[x].selectorText == className) {
            cssText += classes[x].cssText || classes[x].style.cssText;
        }
    }
    return cssText;
}

// compare object properties and return true if they are equal or false if not
function objectsAreEqual(obj1, obj2)
{
    /*Make sure the object is of the same type as this*/
    if (typeof obj1 != typeof obj2)
        return false;

    /*Iterate through the properties of this object looking for a discrepancy between this and obj*/
    for(var property in obj1)
    {
        /*Return false if obj doesn't have the property or if its value doesn't match this' value*/
        if(typeof obj2[property] == "undefined")
            return false;   
        if(obj2[property] != obj1[property])
            return false;
    }

    /*Object's properties are equivalent */
    return true;
}
