var opts = {
    method: 'GET',
    headers: {}
};
fetch('http://ronniepc:8081/all/0/1599999999', opts).then(function (response) {
    return response.json();
}).then(function (body) {
    var json_objects = [];
    body.forEach(mainobject => {
        if (mainobject.length > 0) {
            mainobject.forEach(value => {
                json_objects.push(value);
            });
        }
    });
    console.log(json_objects);
});