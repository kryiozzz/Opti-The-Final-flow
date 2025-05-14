// Cart Page JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // Initialize cart count when page loads
    fetch('/CustomerDashboard/GetCartCount')
        .then(response => response.json())
        .then(data => {
            updateCartCount(data.count);
        })
        .catch(error => console.error('Error loading cart count:', error));

    // Setup select all checkbox functionality
    const selectAllCheckbox = document.createElement('input');
    selectAllCheckbox.type = 'checkbox';
    selectAllCheckbox.className = 'item-checkbox';
    const headerCell = document.querySelector('thead th:first-child');
    if (headerCell) {
        headerCell.appendChild(selectAllCheckbox);

        selectAllCheckbox.addEventListener('change', function () {
            const checkboxes = document.querySelectorAll('.item-checkbox');
            checkboxes.forEach(checkbox => {
                checkbox.checked = this.checked;
            });
        });
    }

    // Add pulse animation for cart count
    const style = document.createElement('style');
    style.textContent = `
        @keyframes pulse {
            0% { transform: scale(1); }
            50% { transform: scale(1.2); }
            100% { transform: scale(1); }
        }
        .pulse {
            animation: pulse 0.5s ease-in-out;
        }
    `;
    document.head.appendChild(style);
});

// Function to update cart count display
function updateCartCount(count) {
    const cartCountElement = document.querySelector('.cart-count');
    if (cartCountElement) {
        cartCountElement.textContent = count;

        // Add animation
        cartCountElement.classList.add('pulse');
        setTimeout(() => {
            cartCountElement.classList.remove('pulse');
        }, 1000);
    }
}

// Update item quantity
// Function to update order quantity
function updateQuantity(orderId, change) {
    // Get the current quantity element
    const qtyElement = document.querySelector(`tr[data-order-id="${orderId}"] .qty-value`);
    const currentQty = parseInt(qtyElement.innerText);
    const newQty = currentQty + change;

    // Prevent negative quantities
    if (newQty <= 0 && change < 0) {
        if (confirm("Remove this item from your cart?")) {
            removeOrder(orderId);
        }
        return;
    }

    // Show loading state
    qtyElement.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

    // Send AJAX request to update quantity
    fetch(`/CustomerOrders/UpdateQuantity/${orderId}?change=${change}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update the quantity display
                qtyElement.innerText = data.newQuantity;

                // Update price cell
                const priceCell = document.querySelector(`tr[data-order-id="${orderId}"] td:nth-child(4)`);
                priceCell.innerText = `₱${data.newTotal.toFixed(2)}`;

                // Update order summary
                updateOrderSummary();

                // Show success message
                showToast(data.message, 'success');
            } else {
                // Revert to original quantity
                qtyElement.innerText = currentQty;
                showToast(data.message, 'error');
            }
        })
        .catch(error => {
            console.error('Error updating quantity:', error);
            qtyElement.innerText = currentQty;
            showToast('Failed to update quantity. Please try again.', 'error');
        });
}

// Function to remove an order
function removeOrder(orderId) {
    if (!confirm('Are you sure you want to remove this item?')) {
        return;
    }

    const row = document.querySelector(`tr[data-order-id="${orderId}"]`);
    row.style.opacity = '0.5';

    fetch(`/CustomerOrders/RemoveFromCart/${orderId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Fade out and remove the row
                row.style.transition = 'opacity 0.3s ease';
                row.style.opacity = '0';

                setTimeout(() => {
                    row.remove();

                    // Check if cart is now empty
                    const tableRows = document.querySelectorAll('.cart-table tbody tr');
                    if (tableRows.length === 0) {
                        document.querySelector('.cart-table tbody').innerHTML = `
                        <tr>
                            <td colspan="5">
                                <div class="empty-cart">
                                    <i class="fas fa-clipboard-list empty-cart-icon"></i>
                                    <h2 class="empty-cart-title">No Orders Found</h2>
                                    <p class="empty-cart-message">There are currently no customer orders in the system.</p>
                                    <a href="/CustomerDashboard/Index" class="browse-btn">
                                        <i class="fas fa-shopping-bag"></i> Browse Products
                                    </a>
                                </div>
                            </td>
                        </tr>
                    `;
                    }

                    // Update order summary
                    updateOrderSummary();

                    showToast(data.message, 'success');
                }, 300);
            } else {
                row.style.opacity = '1';
                showToast(data.message, 'error');
            }
        })
        .catch(error => {
            console.error('Error removing order:', error);
            row.style.opacity = '1';
            showToast('Failed to remove item. Please try again.', 'error');
        });
}

// Function to update the order summary
function updateOrderSummary() {
    // Count total orders
    const totalOrders = document.querySelectorAll('.order-checkbox').length;

    // Calculate total items
    let totalItems = 0;
    document.querySelectorAll('.qty-value').forEach(el => {
        totalItems += parseInt(el.innerText);
    });

    // Calculate total revenue
    let totalRevenue = 0;
    document.querySelectorAll('tr[data-order-id]').forEach(row => {
        const priceText = row.querySelector('td:nth-child(4)').innerText;
        const price = parseFloat(priceText.replace('₱', '').replace(',', ''));
        totalRevenue += price;
    });

    // Update summary values
    document.querySelector('.summary-row:nth-child(2) .summary-value').innerText = totalOrders;
    document.querySelector('.summary-row:nth-child(3) .summary-value').innerText = totalItems;
    document.querySelector('.summary-total .summary-value').innerText = `₱${totalRevenue.toFixed(2)}`;
}

// Function to show toast messages
function showToast(message, type = 'info') {
    // Check if toast container exists, if not create it
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.style.position = 'fixed';
        toastContainer.style.top = '20px';
        toastContainer.style.right = '20px';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }

    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.style.minWidth = '250px';
    toast.style.margin = '10px';
    toast.style.padding = '15px';
    toast.style.borderRadius = '5px';
    toast.style.boxShadow = '0 2px 10px rgba(0,0,0,0.2)';
    toast.style.backgroundColor = type === 'success' ? '#4caf50' :
        type === 'error' ? '#f44336' : '#2196f3';
    toast.style.color = 'white';
    toast.style.opacity = '0';
    toast.style.transition = 'opacity 0.3s ease';

    toast.innerText = message;

    // Add toast to container
    toastContainer.appendChild(toast);

    // Fade in
    setTimeout(() => {
        toast.style.opacity = '1';
    }, 10);

    // Remove after 3 seconds
    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => {
            toast.remove();
        }, 300);
    }, 3000);
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Add CSRF token to the page if not present
    if (!document.querySelector('input[name="__RequestVerificationToken"]')) {
        const csrfInput = document.createElement('input');
        csrfInput.type = 'hidden';
        csrfInput.name = '__RequestVerificationToken';
        csrfInput.value = ''; // This should be set server-side
        document.body.appendChild(csrfInput);
    }
});

// Remove item from cart
function removeFromCart(productId) {
    if (confirm('Are you sure you want to remove this item from your cart?')) {
        fetch('/CustomerDashboard/RemoveFromCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `productId=${productId}`
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Update cart count badge
                    updateCartCount(data.itemCount);

                    let row = document.querySelector(`tr[data-product-id="${productId}"]`);
                    if (row) {
                        // Add fade out animation
                        row.style.transition = 'opacity 0.3s ease';
                        row.style.opacity = '0';

                        // Remove after animation completes
                        setTimeout(() => {
                            row.remove();
                        }, 300);
                    }

                    // Update total price
                    const summaryItemValues = document.querySelectorAll('.summary-item-value');
                    if (summaryItemValues.length > 0) {
                        summaryItemValues[0].textContent = `₱${data.cartTotal}`;
                    }

                    const summaryTotalValue = document.querySelector('.summary-total-value');
                    if (summaryTotalValue) {
                        summaryTotalValue.textContent = `₱${data.cartTotal}`;
                    }

                    // If no items left, show empty cart message
                    if (data.itemCount === 0) {
                        setTimeout(() => {
                            location.reload();
                        }, 500);
                    }

                    // Show success toast
                    showToast('Item removed from cart', 'success');
                } else {
                    // Check if login is required
                    if (data.requireLogin) {
                        showLoginPrompt(data.message);
                    } else {
                        showToast(data.message, 'error');
                    }
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showToast('Error removing item from cart', 'error');
            });
    }
}

// Show login modal or redirect to login page
function showLoginPrompt(message) {
    if (confirm(message + " Would you like to login now?")) {
        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
    }
}

// Checkout function
function checkout() {
    // Get all selected items
    const selectedItems = [];
    document.querySelectorAll('.item-checkbox:checked').forEach(checkbox => {
        const row = checkbox.closest('tr');
        const productId = row.dataset.productId;
        selectedItems.push(productId);
    });

    if (selectedItems.length === 0) {
        showToast('Please select at least one item to checkout', 'error');
        return;
    }

    // Create a form to submit the selected items
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/CustomerDashboard/Checkout';

    // Add selected item IDs to form
    selectedItems.forEach(productId => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'productIds';
        input.value = productId;
        form.appendChild(input);
    });

    // Add form to document and submit
    document.body.appendChild(form);
    form.submit();
}

// Toast notification system
function showToast(message, type = 'success') {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.style.position = 'fixed';
        toastContainer.style.bottom = '20px';
        toastContainer.style.right = '20px';
        toastContainer.style.zIndex = '1050';
        document.body.appendChild(toastContainer);
    }

    // Create toast
    const toast = document.createElement('div');
    toast.className = `toast ${type === 'success' ? 'bg-success' : 'bg-danger'} text-white`;
    toast.style.minWidth = '250px';
    toast.style.margin = '10px';
    toast.style.padding = '15px';
    toast.style.borderRadius = '5px';
    toast.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.2)';
    toast.style.display = 'flex';
    toast.style.alignItems = 'center';
    toast.style.justifyContent = 'space-between';

    // Add toast content
    toast.innerHTML = `
        <div>
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2"></i>
            ${message}
        </div>
        <button type="button" class="btn-close btn-close-white" aria-label="Close"></button>
    `;

    // Add to container
    toastContainer.appendChild(toast);

    // Add close button functionality
    const closeButton = toast.querySelector('.btn-close');
    closeButton.addEventListener('click', function () {
        toast.remove();
    });

    // Auto-remove after 3 seconds
    setTimeout(() => {
        if (toast.parentNode) {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.5s ease';

            setTimeout(() => {
                if (toast.parentNode) {
                    toast.remove();
                }
            }, 500);
        }
    }, 3000);
}

// Add these functions to your site.js or create a new cart.js file

// Function to handle user logout with cart clearing
function logoutWithCartClear() {
    // Show loading indicator or disable the logout button
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.disabled = true;
        logoutBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Logging out...';
    }

    // Call the ClearCart endpoint
    fetch('/CustomerOrders/ClearCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        }
    })
        .then(response => {
            // Regardless of response, proceed with logout
            window.location.href = '/Account/Logout';
        })
        .catch(error => {
            console.error('Error clearing cart:', error);
            // Still proceed with logout even if cart clearing fails
            window.location.href = '/Account/Logout';
        });

    // Prevent the default link behavior
    return false;
}

// Attach the logout function to the logout button when the page loads
document.addEventListener('DOMContentLoaded', function () {
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', function (e) {
            e.preventDefault();
            logoutWithCartClear();
        });
    }
});

// Update the cart indicator to show 0 when user logs out
function clearCartIndicator() {
    const cartCountElement = document.querySelector('.order-count');
    if (cartCountElement) {
        cartCountElement.textContent = '0';
    }
}

// cart.js - For handling cart functionality
document.addEventListener('DOMContentLoaded', function () {
    // Initialize cart count when page loads for authenticated users
    updateCartCount();

    // Setup logout button
    enhanceLogoutButton();
});

// Function to update the cart count display
function updateCartCount() {
    const cartCountElement = document.querySelector('.order-count');
    if (cartCountElement) {
        fetch('/CustomerOrders/GetOrderCount')
            .then(response => response.json())
            .then(data => {
                cartCountElement.textContent = data.count;
                // Add pulse animation if count changes
                cartCountElement.classList.add('pulse');
                setTimeout(() => {
                    cartCountElement.classList.remove('pulse');
                }, 1000);
            })
            .catch(error => {
                console.error('Error loading cart count:', error);
            });
    }
}

// Add visual feedback during logout
function enhanceLogoutButton() {
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', function (e) {
            // Show visual feedback during logout
            const originalText = this.innerHTML;
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Logging out...';
            this.classList.add('disabled');

            // If logout takes longer than expected, restore button
            setTimeout(() => {
                if (!this.classList.contains('disabled')) return;
                this.innerHTML = originalText;
                this.classList.remove('disabled');
            }, 3000);
        });
    }
}

// Function to add items to cart - called from product pages
function addToCart(productId, quantity = 1) {
    fetch('/CustomerOrders/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `productId=${productId}&quantity=${quantity}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Show success message
                showToast(data.message, 'success');

                // Update cart count
                updateCartCount();
            } else {
                // Check if login is required
                if (data.requireLogin) {
                    if (confirm(data.message + " Would you like to login now?")) {
                        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                    }
                } else {
                    showToast(data.message, 'error');
                }
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('Error adding to cart', 'error');
        });
}

// Toast notification function
function showToast(message, type = 'success') {
    // Check if toast container exists, create if not
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.style.position = 'fixed';
        toastContainer.style.bottom = '20px';
        toastContainer.style.right = '20px';
        toastContainer.style.zIndex = '1050';
        document.body.appendChild(toastContainer);
    }

    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast ${type === 'success' ? 'bg-success' : 'bg-danger'} text-white`;
    toast.style.minWidth = '250px';
    toast.style.margin = '10px';
    toast.style.padding = '15px';
    toast.style.borderRadius = '5px';
    toast.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.2)';
    toast.style.display = 'flex';
    toast.style.alignItems = 'center';
    toast.style.justifyContent = 'space-between';

    // Toast content
    toast.innerHTML = `
        <div>
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2"></i>
            ${message}
        </div>
        <button type="button" class="btn-close btn-close-white" aria-label="Close"></button>
    `;

    // Add to container
    toastContainer.appendChild(toast);

    // Add close functionality
    const closeButton = toast.querySelector('.btn-close');
    closeButton.addEventListener('click', function () {
        toast.remove();
    });

    // Auto-dismiss after 3 seconds
    setTimeout(() => {
        if (toast.parentNode) {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.5s ease';

            setTimeout(() => {
                if (toast.parentNode) {
                    toast.remove();
                }
            }, 500);
        }
    }, 3000);
}