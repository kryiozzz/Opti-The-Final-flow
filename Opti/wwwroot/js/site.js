
// site.js - Updated with logout functionality

// Document ready function
$(document).ready(function () {
    // Initialize Bootstrap components
    initBootstrapComponents();

    // Initialize cart functionality
    initCartFunctionality();

    // Initialize logout functionality
    initLogoutFunctionality();
});

// Initialize Bootstrap components
function initBootstrapComponents() {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Initialize dropdowns
    var dropdownElementList = [].slice.call(document.querySelectorAll('.dropdown-toggle'));
    var dropdownList = dropdownElementList.map(function (dropdownToggleEl) {
        return new bootstrap.Dropdown(dropdownToggleEl);
    });
}

// Initialize cart functionality
function initCartFunctionality() {
    // This will be implemented in cart.js
}

// Initialize logout functionality
function initLogoutFunctionality() {
    // Handle the logout button click
    $('#logoutBtn').on('click', function (e) {
        e.preventDefault();

        // Create a form to submit POST request for logout
        var form = $('<form></form>').attr({
            method: 'post',
            action: $(this).attr('href')
        });

        // Add anti-forgery token if needed
        var token = $('input[name="__RequestVerificationToken"]').val();
        if (token) {
            form.append($('<input>').attr({
                type: 'hidden',
                name: '__RequestVerificationToken',
                value: token
            }));
        }

        // Append form to body and submit
        $('body').append(form);
        form.submit();
    });
}

// Updated site.js with proper initialization of Bootstrap components

// Document ready function
$(document).ready(function () {
    console.log("Document ready");

    // Initialize Bootstrap dropdowns
    initializeDropdowns();

    // Initialize Bootstrap tooltips
    initializeTooltips();

    // Handle logout button click
    handleLogout();
});

// Initialize Bootstrap dropdowns
function initializeDropdowns() {
    // First try the new Bootstrap 5 way
    if (typeof bootstrap !== 'undefined') {
        console.log("Initializing dropdowns with Bootstrap 5");
        var dropdownElementList = [].slice.call(document.querySelectorAll('.dropdown-toggle'));
        dropdownElementList.forEach(function (dropdownToggleEl) {
            new bootstrap.Dropdown(dropdownToggleEl);
        });
    }
    // Fallback to jQuery for Bootstrap 4
    else if (typeof $.fn.dropdown !== 'undefined') {
        console.log("Initializing dropdowns with jQuery");
        $('.dropdown-toggle').dropdown();
    }
    else {
        console.warn("Bootstrap dropdown functionality not found");
    }
}

// Initialize Bootstrap tooltips
function initializeTooltips() {
    // First try the new Bootstrap 5 way
    if (typeof bootstrap !== 'undefined') {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(function (tooltipTriggerEl) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
    // Fallback to jQuery for Bootstrap 4
    else if (typeof $.fn.tooltip !== 'undefined') {
        $('[data-toggle="tooltip"]').tooltip();
    }
}

// Handle logout button click
function handleLogout() {
    // This is now handled by the form in the layout
    // The button is now a submit button inside a form

    // Add a click event to the logout link just in case it's not in a form
    $('#logoutBtn').on('click', function (e) {
        console.log("Logout button clicked");
        e.preventDefault();

        // Create and submit a form
        $('<form>')
            .attr({
                method: 'post',
                action: $(this).attr('href') || '/Account/Logout'
            })
            .appendTo('body')
            .submit();
    });
}