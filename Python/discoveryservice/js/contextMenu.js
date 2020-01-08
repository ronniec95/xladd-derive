"use strict";

// *** CONTEXT MENU

const menu = document.querySelector(".menu");
//const menuOption = document.querySelectorAll(".menu-option");
var menuVisible = false;

//$('body').on('contextmenu', '#canvasBase', function(e) {
//    return false;
//});

//console.log(menuOption);

const toggleMenu = command => {
    //console.log("toggleMenu");
    menu.style.display = command === "show" ? "block" : "none";
    menuVisible = !menuVisible;
};

const setPosition = ({ top, left }) => {
    menu.style.left = `${left}px`;
    menu.style.top = `${top}px`;
    toggleMenu("show");
};

window.addEventListener("click", e => {
    if (menuVisible)
        toggleMenu("hide");

    // delete some stuff??
});

// do something more - check for if on canvas but not on ...
window.addEventListener("contextmenu", e => {

    //console.log("2: ", menuVisible);
    
    if (menuVisible) {
        //console.log("2: default prevented - contextmenu");
        e.preventDefault();
    }

    // only prevent default in certain situations.. which comes first?
    //console.log("2");

    //  const origin = {
    //    left: e.pageX,
    //    top: e.pageY
    //  };
    //  setPosition(origin);
    //  return false;
});


function contextMenuEvent(obj, event) {
    
    console.log(event, obj);
    
    switch (event) {
        case "Select edge":
        case "Select node":
            obj.Selected = true;
            break;

        case "Select dependencies":
            for (var d of graph.getDependencyNodeIds(obj.Id))
                graph.getNode(d).Selected = true;
            break;

        case "Select dependants":
            for (var d of graph.getDependantNodeIds(obj.Id))
                graph.getNode(d).Selected = true;
            break;
            
        case "Deselect edge":
        case "Deselect node":
            obj.Selected = false;
            break;

        case "Delete node":
            // this is a node so can simply remove it from the graph
            if (obj instanceof GraphNode)
                graph.deleteNode(obj);
            break;

        case "Delete edge":
            // this is an edge so can simply remove it from the graph
            if (obj instanceof GraphEdge)
                graph.deleteEdge(obj);
            break;
        
        case "Delete edges":
            if (obj instanceof GraphNodeConnector) {
                // a connector - so get any edges connected to this connector
                for (var edge of graph.getEdges(obj))
                    graph.deleteEdge(edge);
            }
            else if (obj instanceof GraphNode) {
                // a node - so get any edges connected to this node
                for (var edge of graph.getEdges(obj))
                    graph.deleteEdge(edge);
            }
            break;
    }
    
    graph.draw(ctx);
}

function setupContextMenu(hoverNode) {
    if (hoverNode instanceof GraphNode) {
        menu.innerHTML = '<ul class="menu-options">' +
            '<li class="menu-option">Select node</li>' +
            '<li class="menu-option">Deselect node</li>' +
            '<li class="menu-option-separator">-</li>' +
            '<li class="menu-option">Delete node</li>' +
            '<li class="menu-option">Delete edges</li>' +
            '<li class="menu-option-separator">-</li>' +
            '<li class="menu-option">Select dependencies</li>' +
            '<li class="menu-option">Select dependants</li>' +
            '</ul>';
    } else if (hoverNode instanceof GraphNodeConnector) {
        menu.innerHTML = '<ul class="menu-options">' +
            '<li class="menu-option">Delete edges</li>' +
            '</ul>';
    } else if (hoverNode instanceof GraphEdge) {
        menu.innerHTML = '<ul class="menu-options">' +
            '<li class="menu-option">Select edge</li>' +
            '<li class="menu-option">Deselect edge</li>' +
            '<li class="menu-option-separator">-</li>' +
            '<li class="menu-option">Delete edge</li>' +
            '</ul>';
    }

    let menuOption = document.querySelectorAll(".menu-option");
    for (const mOpt of menuOption) {
        mOpt.addEventListener('click', e => {
            contextMenuEvent(hoverNode, e.target.innerHTML);
        });
    }
}

// *** END CONTEXT MENU

