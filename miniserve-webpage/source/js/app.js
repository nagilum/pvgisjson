"use strict";

let apiBaseUrl = 'http://localhost:50381/',
    searchInput,
    searchResults,
    searchSummary,
    searchTimeout;

/**
 * Perform the actual search.
 */
let performSearch = () => {
    return fetch(apiBaseUrl + 'v1/search',
        {
            body: JSON.stringify({
                query: searchInput.value.trim()
            }),
            contentType: 'application/json',
            method: 'POST'
        })
        .then((res) => {
            if (res.status !== 200) {
                throw new Error(res.statusText);
            }

            return res.json();
        })
        .then((results) => {
            // Populate the search results to interface.
            populateSearchResults(results);
        })
        .catch((err) => {
            console.log(err);
            alert(err);
        });
};

/**
 * Populate the search results to interface.
 * @param {JSON} results 
 */
let populateSearchResults = (results) => {
    searchSummary.setText(
        'Commodities: ' + results.meta.commodities + ', ' +
        'Modules: ' + results.meta.modules + ', ' +
        'Systems: ' + results.meta.systems
    );

    // Clean out the search results element.
    searchResults.empty();

    // Populate the commodities from search results.
    populateSearchResultsCommodities(results.data.commodities);

    // Populate the modules from search results.
    populateSearchResultsModules(results.data.modules);

    // Populate the systems from search results.
    populateSearchResultsSystems(results.data.systems);
};

/**
 * Populate the commodities from search results.
 * @param {JSON} commodities 
 */
let populateSearchResultsCommodities = (commodities) => {
    if (commodities.length === 0) {
        return;
    }

    let ul = jk('<ul>').addClass('commodities');

    commodities.forEach((commodity) => {
        let li = jk('<li>'),
            title = jk('<span>'),
            category = jk('<span>'),
            averagePrice = jk('<span>'),
            isRare = jk('<span>');
        
        title
            .addClass('title')
            .setText(commodity.name);
        
        category
            .addClass('category')
            .setText('(' + commodity.category_name + ')');
        
        averagePrice
            .addClass('attribute')
            .setText('Average Price: ' + commodity.average_price + ' CR');
        
        isRare
            .addClass('is-rare')
            .setText('Rare');
        
        li
            .append(title)
            .append(category)
            .append(jk('<br>'))
            .append(averagePrice);

        if (commodity.is_rare) {
            li.append(isRare);
        }

        ul.append(li);
    });

    searchResults
        .append(jk('<h2>').setText('Commodities'))
        .append(ul);
};

/**
 * Populate the modules from search results.
 * @param {JSON} modules 
 */
let populateSearchResultsModules = (modules) => {
    if (modules.length === 0) {
        return;
    }

    let ul = jk('<ul>').addClass('modules');

    modules.forEach((module) => {
        let li = jk('<li>'),
            title = jk('<span>'),
            category = jk('<span>');
        
        title
            .addClass('title')
            .setText(module.ser_class + module.rating + ' ' + module.group_name);
        
        category
            .addClass('category')
            .setText('(' + module.group_category_name + ')');

        li
            .append(title)
            .append(category);
        
        let attributes = [];

        if (module.missile_type) {
            attributes.push('Missile Type: ' + module.missile_type);
        }

        if (module.weapon_mode) {
            attributes.push('Weapon Mode: ' + module.weapon_mode);
        }

        if (module.ammo) {
            attributes.push('Ammo: ' + module.ammo);
        }

        if (module.damage) {
            attributes.push('Damage: ' + module.damage);
        }

        if (module.dps) {
            attributes.push('DPS: ' + module.dps);
        }

        if (module.mass) {
            attributes.push('Mass: ' + module.mass);
        }

        if (module.power) {
            attributes.push('Power: ' + module.power);
        }

        if (module.price) {
            attributes.push('Price: ' + module.price + ' CR');
        }

        if (attributes.length > 0) {
            li.append(jk('<br>'));

            let count = 0;

            attributes.forEach((attr) => {
                li.append(jk('<span>').addClass('attribute').setText(attr));

                count++;

                if (count < attributes.length) {
                    li.append(jk('<span>').addClass('attribute').setText(' - '));
                }
            });
        }

        ul.append(li);
    });

    searchResults
        .append(jk('<h2>').setText('Modules'))
        .append(ul);
};

/**
 * Populate the systems from search results.
 * @param {JSON} systems 
 */
let populateSearchResultsSystems = (systems) => {
    if (systems.length === 0) {
        return;
    }

    let ul = jk('<ul>').addClass('systems');

    systems.forEach((system) => {
        console.log(system);

        let li = jk('<li>'),
            name = jk('<span>'),
            allegiance = jk('<span>'),
            attributes = [];
        
        name
            .addClass('title')
            .setText(system.name);
        
        allegiance
            .addClass('category')
            .setText(system.allegiance);
        
        if (system.controlling_minor_faction) {
            attributes.push('Faction: ' + system.controlling_minor_faction);
        }

        if (system.government) {
            attributes.push('Government: ' + system.government);
        }

        if (system.primary_economy) {
            attributes.push('Economy: ' + system.primary_economy);
        }

        if (system.security) {
            attributes.push('Security: ' + system.security);
        }
        
        li
            .append(name)
            .append(allegiance);
        
        if (attributes.length > 0) {
            li.append(jk('<br>'));

            let count = 0;

            attributes.forEach((attr) => {
                li.append(jk('<span>').addClass('attribute').setText(attr));

                count++;

                if (count < attributes.length) {
                    li.append(jk('<span>').addClass('attribute').setText(' - '));
                }
            });
        }

        ul.append(li);
    });

    searchResults
        .append(jk('<h2>').setText('Systems'))
        .append(ul);
};

/**
 * Check if input box meets criteria for search and init it.
 */
let triggerSearch = () => {
    let str = searchInput.value.trim();

    if (!str ||
        str.length < 2) {
        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        return;
    }

    if (searchTimeout) {
        clearTimeout(searchTimeout);
    }

    // Perform the actual search if no other keystrokes comes in inside the next 250 ms.
    searchTimeout = setTimeout(performSearch, 250);
};

/**
 * Init all the stuff.
 */
jk(() => {
    searchInput = jk('input#search');
    searchResults = jk('results');
    searchSummary = jk('summary');

    searchInput
        .on('keyup', triggerSearch)
        .focus();
});