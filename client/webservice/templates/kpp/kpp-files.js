//# sourceURL=kpp-files.js
function tree_toggle(event) {
    event = event || window.event
    var clickedElem = event.target || event.srcElement

    if (!hasClass(clickedElem, 'tree-Expand')) {
        return // клик не там
    }

    // Node, на который кликнули
    var node = clickedElem.parentNode
    if (hasClass(node, 'tree-ExpandLeaf')) {
        return // клик на листе
    }

    // определить новый класс для узла
    var newClass = hasClass(node, 'tree-ExpandOpen') ? 'tree-ExpandClosed' : 'tree-ExpandOpen'
        // заменить текущий класс на newClass
        // регексп находит отдельно стоящий open|close и меняет на newClass
    var re = /(^|\s)(tree-ExpandOpen|tree-ExpandClosed)(\s|$)/
    node.className = node.className.replace(re, '$1' + newClass + '$3')
}


function hasClass(elem, className) {
    return new RegExp("(^|\\s)" + className + "(\\s|$)").test(elem.className)
}



$(document).ready(function() {


    $('#plus-tab').on('click', function() {
        $('#addInfoRow').collapse('show');
    })

    $('#btnExtension').on('click', function() {
        $('#sectionExtension').collapse('toggle');
    })

    // $('#treeStart').on('click',function(event){
    //     tree_toggle(event);
    // })



})