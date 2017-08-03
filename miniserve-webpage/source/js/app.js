"use strict";

/**
 * Compile the request, update the UI, and sende the request.
 */
var compileAndSendRequest = () => {
    var url = 'https://pvgisjson.com/api/v1/pv',
        values = [],
        payload = {};
    
    // method
    var method = jk('select#method').value,
        isPOST = method === 'POST';
    
    // lat
    var lat = parseFloat(jk('input#lat').value);

    if (isPOST) {
        payload.lat = lat;
    }
    else {
        values.push('lat=' + lat);
    }

    // lng
    var lng = parseFloat(jk('input#lng').value);

    if (isPOST) {
        payload.lng = lng;
    }
    else {
        values.push('lng=' + lng);
    }

    // pvtech
    var pvtech = jk('select#pvtech').value;
    
    if (isPOST) {
        payload.pvtech = pvtech;
    }
    else {
        values.push('pvtech=' + pvtech);
    }

    // peakpower
    var peakpower = parseFloat(jk('input#peakpower').value);

    if (isPOST) {
        payload.peakpower = peakpower;
    }
    else {
        values.push('peakpower=' + peakpower);
    }

    // losses
    var losses = parseFloat(jk('input#losses').value);

    if (isPOST) {
        payload.losses = losses;
    }
    else {
        values.push('losses=' + losses);
    }

    // mounting
    var mounting = jk('select#mounting').value;
    
    if (isPOST) {
        payload.mounting = mounting;
    }
    else {
        values.push('mounting=' + mounting);
    }

    // slope
    var slope = parseFloat(jk('input#slope').value);

    if (isPOST) {
        payload.slope = slope;
    }
    else {
        values.push('slope=' + slope);
    }

    // azimuth
    var azimuth = parseFloat(jk('input#azimuth').value);

    if (isPOST) {
        payload.azimuth = azimuth;
    }
    else {
        values.push('azimuth=' + azimuth);
    }

    // Update the UI.
    if (isPOST) {
        jk('pre#url').setText('POST ' + url);
        jk('pre#payload').removeClass('italic').setText(JSON.stringify(payload, null, '  '));
    }
    else {
        url = url + '?' + values.join('&');

        jk('pre#url').setText('GET ' + url);
        jk('pre#payload').addClass('italic').setText('None');
    }

    // Perform the request.
    var options = {};

    if (isPOST) {
        options.method = 'POST';
        options.body = JSON.stringify(payload);
    }
    else {
        options.method = 'GET';
    }

    fetch(url, options)
        .then((res) => {
            console.log(res);

            if (res.status !== 200) {
                throw new Error(res.statusText);
            }

            return res.json();
        })
        .then((json) => {
            jk('pre#response').setText(JSON.stringify(json, null, '  '));
        })
        .catch((err) => {
            alert(err);
        });
};

/**
 * Init all the stuff..
 */
jk(() => {
    jk('input#fetch').on('click', compileAndSendRequest);
});