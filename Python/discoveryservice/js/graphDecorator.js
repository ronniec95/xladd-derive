"use strict";

var _decorator;

class GraphDecorator {

    static getDecorator() {
        
        if (_decorator) {
            return _decorator;
        }
        
        //console.log("GetDecorator");

        var titleElement = document.getElementsByClassName("aarcNode-title")[0];
        let ss = window.getComputedStyle(titleElement);
        var fontWeight = ss.fontWeight;//('font-weight');
        var fontSize = ss.fontSize;//('font-size');
        var fontFamily = ss.fontFamily;//('font-family');
        var fontColor = ss.color;//('color');
        var textShadow = null;

        let smallfontSize = parseInt(fontSize.substr(0, fontSize.length - 2)) - 1;

        if (ss.textShadow) {
            var res = ss.textShadow.split(' ');
            var len = res.length;

            textShadow = {};
            textShadow.shadowColor = ss.textShadow.substr(0, ss.textShadow.lastIndexOf(')'));
            textShadow.shadowOffsetX = res[len-3].substr(0, res[len-3].length-2);
            textShadow.shadowOffsetY = res[len-2].substr(0, res[len-2].length-2);
            textShadow.shadowBlur = res[len-1].substr(0, res[len-1].length-2);
        }

        console.log(fontWeight, fontSize, fontFamily, fontColor, textShadow);
        
        _decorator = {
            fontWeight: fontWeight,
            fontSize: fontSize,
            fontFamily: fontFamily,
            fontColor: fontColor,
            smallfontSize: smallfontSize,
            textShadow: textShadow
        };
        
        return _decorator;
    }
    
    /*
    Theme1: ffeedd 4a5d9e d76428 2e3532 8b2635
    papaya whipe, liberty, vivid red-tangelo, jet, japanese carmine
    also: dim gray 776d63, alabama crimson b2031d - and shades lighter: e62743
    */
    
    static setStyle(ctx, type) {

        switch (type) {
            case "edgeShadow":
            case "nodeShadow":
//                ctx.shadowOffsetX = 2;
//                ctx.shadowOffsetY = 2;
//                ctx.shadowColor = "#997766"; // and e.g. 654 ca8 etc
//                ctx.shadowBlur = 4;
                ctx.shadowOffsetX = 1;
                ctx.shadowOffsetY = 1;
                ctx.shadowColor = "rgba(0,0,0,0.3)";
                ctx.shadowBlur = 4;
                break;

            case "nodeLineWidth:selected":
                ctx.lineWidth = 2.5;
                break;
                
            case "nodeLineWidth":
                ctx.lineWidth = 1.3;
                break;
        }
    }

    static getStyle(type) {
        switch (type) {
            case "selected":
                return "#ff5700";
            // "#86bd0f"; //"rgba(220, 80, 200, 1)"; // "#ffef45"; //"#2bb9cc";// "#ff5700";
            //"#d95990";// "#fa1e05";// "#e62743";
            case "hover":
                return "#fa1e05";
        }
    }
    
    static setFillStyle(ctx, type, element) {

        let grd;
        switch (type) {
                
            case "selected":
                ctx.fillStyle = getStyle(type);
                break;
                
            case "node":
                
//                grd = ctx.createLinearGradient(element.Left, 0, element.Left + element.Width, 0);
//                // maroon
//                grd.addColorStop(1, "#bd2866");
//                grd.addColorStop(0, "#470a24");
                
                //The createRadialGradient() method is specified by six parameters,
                // three defining the gradient's start circle, and three defining the end circle
                // x0, y0, r0, x1, y1, r1
                let xx = element.Left + element.Width / 2;
                let yy = element.Top + element.Height / 2;
                grd = ctx.createRadialGradient(xx, yy, 0, xx, yy, Math.max(element.Width, element.Height) * 1.5);

                // maroon
//                grd.addColorStop(0, "#bd2866");
//                grd.addColorStop(1, "#470a24");

                // maroonish
//                grd.addColorStop(0, "#b2031d");
//                grd.addColorStop(0.5, "#e62743");   // shade up
//                grd.addColorStop(1, "#b2031d");
                
                // blue
                grd.addColorStop(0, "#4a5d9e");
                grd.addColorStop(0.5, "#687cc4");
                grd.addColorStop(1, "#4a5d9e");
                
                ctx.fillStyle = grd;

//                ctx.fillStyle = "#b2031d";
                
                break;

            case "node:selected":
                grd = ctx.createLinearGradient(element.Left, 0, element.Left + element.Width, 0);

                // orange
                grd.addColorStop(0, "rgb(215,100,40)");//"#c95924");//"#4a5d9e");
                grd.addColorStop(0.5, "rgb(255,140,80)");//"#c9734b");//"#687cc4");
                grd.addColorStop(1, "rgb(215,100,40)");//"#c95924");//"#4a5d9e");

                ctx.fillStyle = grd;
                break;
                
            case "node:hover":
                grd = ctx.createLinearGradient(element.Left, 0, element.Left + element.Width, 0);

                // orange red
                grd.addColorStop(0, "#c21e0c");
                grd.addColorStop(0.5, "#fa1e05");
                grd.addColorStop(1, "#c21e0c");

                // blue
//                grd.addColorStop(0, "#4a5d9e");
//                grd.addColorStop(0.5, "#687cc4");
//                grd.addColorStop(1, "#4a5d9e");
                
//                grd.addColorStop(0, "#b2031d");
//                grd.addColorStop(0.5, "#e62743");   // shade up
//                grd.addColorStop(1, "#b2031d");
                
                ctx.fillStyle = grd;
                break;
        }
    }
    
}

