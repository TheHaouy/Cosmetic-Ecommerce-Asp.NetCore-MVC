// ==================== SANPHAM.JS - TRANG SẢN PHẨM ====================
// File này chứa tất cả logic cho trang danh sách sản phẩm
// Version: Clean & Organized

// ==================== MOBILE SIDEBAR TOGGLE ====================
/**
 * Khởi tạo mobile sidebar toggle
 */
function initializeMobileSidebar() {
    const filterToggleBtn = document.getElementById('filterToggleBtn');
    const closeSidebarBtn = document.getElementById('closeSidebarBtn');
    const sidebarMenu = document.getElementById('sidebarMenu');
    const sidebarOverlay = document.getElementById('sidebarOverlay');

    if (filterToggleBtn && sidebarMenu && sidebarOverlay) {
        // Open sidebar
        filterToggleBtn.addEventListener('click', function() {
            sidebarMenu.classList.add('show');
            sidebarOverlay.classList.add('show');
            // Better approach for preventing body scroll on mobile
            const scrollY = window.scrollY;
            document.body.style.position = 'fixed';
            document.body.style.top = `-${scrollY}px`;
            document.body.style.width = '100%';
            document.body.setAttribute('data-scroll-position', scrollY);
        });

        // Close sidebar
        const closeSidebar = function() {
            sidebarMenu.classList.remove('show');
            sidebarOverlay.classList.remove('show');
            // Restore body scroll position
            const scrollY = document.body.getAttribute('data-scroll-position');
            document.body.style.position = '';
            document.body.style.top = '';
            document.body.style.width = '';
            document.body.removeAttribute('data-scroll-position');
            if (scrollY) {
                window.scrollTo(0, parseInt(scrollY));
            }
        };

        if (closeSidebarBtn) {
            closeSidebarBtn.addEventListener('click', closeSidebar);
        }

        // Close when clicking overlay
        sidebarOverlay.addEventListener('click', closeSidebar);

        // Close on window resize if changing from mobile to desktop
        window.addEventListener('resize', function() {
            if (window.innerWidth >= 992) {
                closeSidebar();
            }
        });
    }

    // Initialize category toggle for mobile
    initializeCategoryToggle();
}

/**
 * Khởi tạo toggle cho category có children trên mobile/tablet
 */
function initializeCategoryToggle() {
    const isTouchDevice = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    const isSmallScreen = window.innerWidth <= 991;

    if (isTouchDevice && isSmallScreen) {
        const categoryItemsWithChildren = document.querySelectorAll('.category-item-wrapper.has-children');

        categoryItemsWithChildren.forEach(wrapper => {
            const parentLink = wrapper.querySelector('.category-parent-link');
            const childrenList = wrapper.querySelector('.category-children');

            if (parentLink && childrenList) {
                // Clone để remove old listeners
                const newParentLink = parentLink.cloneNode(true);
                parentLink.parentNode.replaceChild(newParentLink, parentLink);

                newParentLink.addEventListener('click', function(e) {
                    e.preventDefault();
                    e.stopPropagation();

                    // Toggle active state
                    wrapper.classList.toggle('active');

                    // Close other categories
                    categoryItemsWithChildren.forEach(otherWrapper => {
                        if (otherWrapper !== wrapper) {
                            otherWrapper.classList.remove('active');
                        }
                    });
                });

                // Children links work normally
                const childLinks = childrenList.querySelectorAll('a');
                childLinks.forEach(link => {
                    link.addEventListener('click', function(e) {
                        e.stopPropagation();
                        // Link hoạt động bình thường
                    });
                });
            }
        });
    }
}

// ==================== PRICE SLIDER FUNCTIONALITY ====================
/**
 * Khởi tạo price slider
 */
function initializePriceSlider() {
    const priceSlider = document.getElementById('priceSlider');
    const priceDisplay = document.getElementById('priceRangeDisplay');
    const maxPriceValue = document.getElementById('maxPriceValue');
    
    if (priceSlider && priceDisplay && maxPriceValue) {
        // Set initial display
        const initialValue = parseInt(priceSlider.value);
        const formattedValue = initialValue.toLocaleString('vi-VN');
        priceDisplay.textContent = `0₫ - ${formattedValue}₫`;
        
        // Update display when slider changes
        priceSlider.addEventListener('input', function(e) {
            const value = parseInt(e.target.value);
            const formattedValue = value.toLocaleString('vi-VN');
            priceDisplay.textContent = `0₫ - ${formattedValue}₫`;
            maxPriceValue.value = value;
        });
    }
}

// ==================== ADD TO CART FUNCTIONALITY ====================
/**
 * Thêm sản phẩm vào giỏ hàng từ danh sách sản phẩm
 * @param {string|number} productId - ID của sản phẩm
 */
function addToCartFromProductList(productId) {
    // Disable button to prevent double clicks
    const button = document.querySelector(`[data-product-id="${productId}"]`);
    if (button) {
        button.disabled = true;
        button.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Đang thêm...';
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    
    fetch('/KhachHang/Cart/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({
            id: productId,
            quantity: 1
        })
    })
    .then(response => response.json())
    .then(data => {
        // Re-enable button first
        if (button) {
            button.disabled = false;
            button.innerHTML = '<i class="fas fa-shopping-cart"></i> Thêm giỏ hàng';
        }

        if (data.success) {
            // Update cart count in header using global function
            if (typeof updateCartCount === 'function') {
                updateCartCount(data.cartCount);
            } else {
                // Fallback if function not available
                const cartCountElement = document.getElementById('cartCount');
                if (cartCountElement) {
                    cartCountElement.textContent = data.cartCount;
                    cartCountElement.style.display = 'inline';
                }
            }
            
            // Không hiển thị thông báo nữa - chỉ cập nhật số lượng giỏ hàng
        } else if (data.requireLogin) {
            // Show login required modal
            const loginModal = new bootstrap.Modal(document.getElementById('loginRequiredModal'));
            loginModal.show();
        } else {
            // Show error message
            showErrorMessage(data.message || 'Có lỗi xảy ra khi thêm sản phẩm!');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        // Re-enable button
        if (button) {
            button.disabled = false;
            button.innerHTML = '<i class="fas fa-shopping-cart"></i> Thêm giỏ hàng';
        }
        showErrorMessage('Có lỗi xảy ra khi thêm sản phẩm!');
    });
}

/**
 * Legacy function for compatibility
 */
function addToCart(productId) {
    addToCartFromProductList(productId);
}

// ==================== TOAST NOTIFICATIONS ====================
/**
 * Hiển thị thông báo thành công
 * @param {string} message - Nội dung thông báo
 */
function showSuccessMessage(message) {
    createAndShowToast('success', message, 'fa-check-circle');
}

/**
 * Hiển thị thông báo lỗi
 * @param {string} message - Nội dung thông báo
 */
function showErrorMessage(message) {
    createAndShowToast('error', message, 'fa-exclamation-circle');
}

/**
 * Tạo và hiển thị toast notification
 * @param {string} type - Loại toast (success/error)
 * @param {string} message - Nội dung thông báo
 * @param {string} iconClass - Class của icon
 */
function createAndShowToast(type, message, iconClass) {
    const toast = document.createElement('div');
    toast.className = `toast-notification ${type}`;
    toast.innerHTML = `
        <div class="toast-content">
            <i class="fas ${iconClass}"></i>
            <span>${message}</span>
        </div>
    `;
    
    document.body.appendChild(toast);
    
    // Show toast with animation
    setTimeout(() => {
        toast.classList.add('show');
    }, 100);
    
    // Hide toast after 3 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    }, 3000);
}

// ==================== VIEW TOGGLE FUNCTIONALITY ====================
/**
 * Chuyển đổi giữa grid view và list view
 * @param {string} viewType - Loại view ('grid' hoặc 'list')
 */
function toggleView(viewType) {
    const productContainer = document.getElementById('products-container');
    const gridBtn = document.querySelector('.view-btn[data-view="grid"]');
    const listBtn = document.querySelector('.view-btn[data-view="list"]');
    
    if (!productContainer) return;
    
    if (viewType === 'grid') {
        productContainer.classList.remove('list-view');
        productContainer.classList.add('grid-view');
        if (gridBtn) gridBtn.classList.add('active');
        if (listBtn) listBtn.classList.remove('active');
        
        // Update product items for grid view
        document.querySelectorAll('.product-item').forEach(item => {
            item.className = 'col-lg-4 col-md-6 mb-4 product-item';
        });
    } else if (viewType === 'list') {
        productContainer.classList.remove('grid-view');
        productContainer.classList.add('list-view');
        if (listBtn) listBtn.classList.add('active');
        if (gridBtn) gridBtn.classList.remove('active');
        
        // Update product items for list view
        document.querySelectorAll('.product-item').forEach(item => {
            item.className = 'col-12 mb-2 product-item';
        });
    }
    
    // Save preference
    localStorage.setItem('preferredView', viewType);
}

// ==================== INITIALIZATION ====================
/**
 * Khởi tạo tất cả các chức năng khi DOM đã sẵn sàng
 */
document.addEventListener('DOMContentLoaded', function() {
    console.log('SanPham page initialized');
    
    // Initialize mobile sidebar toggle
    initializeMobileSidebar();
    
    // Initialize price slider
    initializePriceSlider();
    
    // Initialize view toggle buttons
    const viewButtons = document.querySelectorAll('.view-btn');
    viewButtons.forEach(btn => {
        btn.addEventListener('click', function() {
            const viewType = this.getAttribute('data-view');
            if (viewType) {
                toggleView(viewType);
            }
        });
    });
    
    // Set default view (from localStorage or default to grid)
    const preferredView = localStorage.getItem('preferredView') || 'grid';
    toggleView(preferredView);
    
    // Add to cart button listeners
    const addToCartButtons = document.querySelectorAll('.btn-add-to-cart');
    addToCartButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const productId = this.getAttribute('data-product-id');
            if (productId) {
                addToCartFromProductList(productId);
            }
        });
    });
    
    // Buy now button listeners
    const buyNowButtons = document.querySelectorAll('.btn-buy-now');
    buyNowButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const productId = this.getAttribute('data-product-id');
            const variantId = this.getAttribute('data-variant-id');
            const hasVariants = this.getAttribute('data-has-variants') === 'true';
            
            if (productId) {
                buyNowFromProductList(productId, variantId, hasVariants);
            }
        });
    });
});

// ==================== BUY NOW FUNCTIONALITY ====================
/**
 * Mua ngay sản phẩm từ danh sách sản phẩm
 * @param {string|number} productId - ID của sản phẩm
 * @param {string|number} variantId - ID của biến thể (0 nếu không có biến thể)
 * @param {boolean} hasVariants - Sản phẩm có biến thể hay không
 */
function buyNowFromProductList(productId, variantId, hasVariants) {
    const quantity = 1; // Luôn mua với số lượng 1
    
    // Nếu có biến thể, dùng variantId được truyền vào (đã là biến thể giá thấp nhất)
    // Nếu không có biến thể, dùng variantId = 0
    const finalVariantId = hasVariants ? (variantId || 0) : 0;
    
    // Chuyển đến trang thanh toán
    const url = `/KhachHang/ChiTiet/MuaNgay?productId=${productId}&variantId=${finalVariantId}&quantity=${quantity}`;
    window.location.href = url;
}
