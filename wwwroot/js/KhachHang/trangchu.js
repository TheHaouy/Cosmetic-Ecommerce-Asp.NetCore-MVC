// Hàm hiển thị thông báo
// Hàm thêm vào giỏ hàng riêng cho TrangChu
function addToCartTrangChu(productId, event) {
    event.preventDefault();
    // Chuyển sang trang ChiTiet sản phẩm
    window.location.href = `/KhachHang/ChiTiet/Index?id=${productId}`;
}
function showNotification(message, type = 'info') {
    // Tạo thông báo
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} alert-dismissible fade show position-fixed`;
    notification.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        max-width: 400px;
    `;
    
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    // Thêm vào body
    document.body.appendChild(notification);
    
    // Tự động xóa sau 3 giây
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 3000);
}

// Quản lý lịch sử tìm kiếm
let searchHistory = JSON.parse(localStorage.getItem('searchHistory')) || [];

// Elements
const searchInput = document.getElementById('searchInput');
const searchBtn = document.getElementById('searchBtn');
const searchSuggestions = document.getElementById('searchSuggestions');
const historyItems = document.getElementById('historyItems');
const clearHistory = document.getElementById('clearHistory');

// Hiển thị lịch sử tìm kiếm
function displaySearchHistory() {
    historyItems.innerHTML = '';
    searchHistory.slice(-5).reverse().forEach(term => {
        const historyItem = document.createElement('span');
        historyItem.className = 'history-item';
        historyItem.textContent = term;
        historyItem.onclick = () => {
            searchInput.value = term;
            performSearch(term);
            hideSearchSuggestions();
        };
        historyItems.appendChild(historyItem);
    });
}

// Hiển thị gợi ý tìm kiếm
function showSearchSuggestions(query = '') {
    searchSuggestions.innerHTML = '';
    
    // Hiển thị lịch sử nếu không có query
    if (!query.trim()) {
        const historyDiv = document.createElement('div');
        historyDiv.className = 'search-history';
        historyDiv.innerHTML = `
            <div class="search-history-title">
                Tìm kiếm gần đây 
                <span class="clear-history" onclick="clearSearchHistory()">Xóa</span>
            </div>
            <div id="historyItemsInner"></div>
        `;
        searchSuggestions.appendChild(historyDiv);
        
        const historyItemsInner = document.getElementById('historyItemsInner');
        searchHistory.slice(-5).reverse().forEach(term => {
            const historyItem = document.createElement('span');
            historyItem.className = 'history-item';
            historyItem.textContent = term;
            historyItem.onclick = () => {
                searchInput.value = term;
                performSearch(term);
                hideSearchSuggestions();
            };
            historyItemsInner.appendChild(historyItem);
        });
    } else {
        // Hiển thị gợi ý sản phẩm
        const filteredProducts = products.filter(product => 
            product.name.toLowerCase().includes(query.toLowerCase()) ||
            product.description.toLowerCase().includes(query.toLowerCase())
        ).slice(0, 8);

        filteredProducts.forEach(product => {
            const suggestionItem = document.createElement('div');
            suggestionItem.className = 'suggestion-item';
            suggestionItem.innerHTML = `
                <div><strong>${product.name}</strong></div>
                <div class="suggestion-category">${getCategoryName(product.category)} • ${formatPrice(product.price)}</div>
            `;
            suggestionItem.onclick = () => {
                searchInput.value = product.name;
                performSearch(product.name);
                hideSearchSuggestions();
            };
            searchSuggestions.appendChild(suggestionItem);
        });

        // Thêm gợi ý danh mục
        const categories = [...new Set(filteredProducts.map(p => p.category))];
        if (categories.length > 0) {
            categories.forEach(category => {
                const categoryItem = document.createElement('div');
                categoryItem.className = 'suggestion-item';
                categoryItem.innerHTML = `
                    <div><i class="fas fa-tag"></i> Xem tất cả trong <strong>${getCategoryName(category)}</strong></div>
                `;
                categoryItem.onclick = () => {
                    // Chuyển đến trang sản phẩm với category filter
                    window.location.href = `sanpham.html?category=${category}`;
                    hideSearchSuggestions();
                };
                searchSuggestions.appendChild(categoryItem);
            });
        }
    }
    
    searchSuggestions.style.display = 'block';
}

function hideSearchSuggestions() {
    searchSuggestions.style.display = 'none';
}

function getCategoryName(category) {
    const categoryNames = {
        'skincare': 'Chăm sóc da',
        'makeup': 'Trang điểm',
        'haircare': 'Dưỡng tóc',
        'suncare': 'Chống nắng',
        'essential-oils': 'Tinh dầu',
        'body-care': 'Chăm sóc cơ thể'
    };
    return categoryNames[category] || category;
}

function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN').format(price) + ' VNĐ';
}

// Thực hiện tìm kiếm
function performSearch(query) {
    if (query.trim()) {
        // Lưu vào lịch sử
        if (!searchHistory.includes(query)) {
            searchHistory.push(query);
            if (searchHistory.length > 10) {
                searchHistory.shift();
            }
            localStorage.setItem('searchHistory', JSON.stringify(searchHistory));
        }
        
        // Hiển thị kết quả tìm kiếm (có thể chuyển hướng đến trang kết quả)
        alert(`Tìm kiếm: "${query}"\nKết quả: ${getSearchResults(query).length} sản phẩm`);
    }
}

function getSearchResults(query) {
    return products.filter(product => 
        product.name.toLowerCase().includes(query.toLowerCase()) ||
        product.description.toLowerCase().includes(query.toLowerCase())
    );
}

function filterByCategory(category) {
    const categoryResults = products.filter(p => p.category === category);
    // Loại bỏ alert để không hiện thông báo
    console.log(`Danh mục: ${getCategoryName(category)}\nCó ${categoryResults.length} sản phẩm`);
}

function clearSearchHistory() {
    searchHistory = [];
    localStorage.removeItem('searchHistory');
    hideSearchSuggestions();
}

// Event listeners
searchInput.addEventListener('input', (e) => {
    const query = e.target.value;
    showSearchSuggestions(query);
});

searchInput.addEventListener('focus', () => {
    showSearchSuggestions(searchInput.value);
});

searchBtn.addEventListener('click', () => {
    performSearch(searchInput.value);
    hideSearchSuggestions();
});

searchInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        performSearch(searchInput.value);
        hideSearchSuggestions();
    }
});

// Hide suggestions when clicking outside
document.addEventListener('click', (e) => {
    if (!e.target.closest('.search-container')) {
        hideSearchSuggestions();
    }
});

// Ngăn không cho suggestions bị ẩn khi click vào chính nó
searchSuggestions.addEventListener('click', (e) => {
    e.stopPropagation();
});

// Category dropdown navigation đã được xử lý trong phần dropdown menu navbar ở trên

// Product interactions
document.addEventListener('DOMContentLoaded', function() {
    // Add to cart functionality
        document.querySelectorAll('.product-actions .btn-primary').forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                const productCard = this.closest('.product-card');
                // Nếu là sản phẩm bán chạy ở TrangChu thì chuyển trang luôn, không hiện thông báo
                if (productCard && productCard.closest('.bestselling-products-section')) {
                    const link = productCard.parentElement;
                    if (link && link.href) {
                        window.location.href = link.href;
                    }
                    return;
                }
                const productName = productCard.querySelector('.product-name').textContent;
                // Show success message
                showNotification(`Đã thêm "${productName}" vào giỏ hàng!`, 'success');
                // Add cart animation
                animateAddToCart(this);
            });
});

// Show notification function
function showNotification(message, type = 'info') {
    // Remove existing notifications
    const existingNotifications = document.querySelectorAll('.custom-notification');
    existingNotifications.forEach(notification => notification.remove());
    
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `custom-notification alert alert-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'}`;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        max-width: 300px;
        animation: slideInRight 0.3s ease;
    `;
    notification.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
        ${message}
        <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
    `;
    
    // Add CSS for animation
    if (!document.querySelector('#notification-styles')) {
        const style = document.createElement('style');
        style.id = 'notification-styles';
        style.textContent = `
            @keyframes slideInRight {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
            .custom-notification {
                display: flex;
                align-items: center;
                gap: 10px;
                padding: 15px;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            }
        `;
        document.head.appendChild(style);
    }
    
    document.body.appendChild(notification);
    
    // Auto remove after 3 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove();
        }
    }, 3000);
}

// Animate add to cart
function animateAddToCart(button) {
    const originalText = button.innerHTML;
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang thêm...';
    button.disabled = true;
    
    setTimeout(() => {
        button.innerHTML = '<i class="fas fa-check"></i> Đã thêm!';
        setTimeout(() => {
            button.innerHTML = originalText;
            button.disabled = false;
        }, 1000);
    }, 500);
}

// Smooth scroll for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// Initialize
displaySearchHistory();

// Hàm lấy đánh giá nổi bật (chỉ 5 sao)
function getFeaturedReviews() {
    // Lọc chỉ những đánh giá 5 sao và sắp xếp theo độ hữu ích
    return featuredReviews
        .filter(review => review.rating === 5)
        .sort((a, b) => b.helpful - a.helpful)
        .slice(0, 3); // Chỉ lấy 3 đánh giá hàng đầu
}

// Hàm tạo HTML cho đánh giá nổi bật
function createFeaturedReviewHTML(review) {
    const formattedDate = new Date(review.date).toLocaleDateString('vi-VN', {
        month: 'long',
        day: 'numeric'
    });
    
    const customerType = review.helpful > 25 ? 'Khách hàng VIP' : 
                        review.helpful > 15 ? 'Khách hàng thân thiết' : 'Khách hàng';
    
    const stars = '⭐'.repeat(review.rating);
    
    return `
        <div class="col-md-4 mb-4">
            <div class="review-card">
                <div class="d-flex align-items-center mb-3">
                    <img src="${review.avatar}" 
                         class="rounded-circle me-3" width="50" height="50" alt="Avatar">
                    <div>
                        <h6 class="mb-0 text-primary-green">${review.userName}</h6>
                        <small class="text-muted">${customerType}</small>
                        <div class="product-name-small text-muted mt-1" style="font-size: 0.75rem;">
                            ${review.productName}
                        </div>
                    </div>
                </div>
                <p class="card-text">"${review.text}"</p>
                <div class="d-flex justify-content-between align-items-center">
                    <div class="stars">${stars}</div>
                    <small class="text-muted">${formattedDate}</small>
                </div>
                ${review.verified ? '<div class="mt-2"><span class="badge bg-success" style="font-size: 0.7rem;">Đã xác minh mua hàng</span></div>' : ''}
            </div>
        </div>
    `;
}

// Hàm cập nhật đánh giá nổi bật
function updateFeaturedReviews() {
    const reviewsContainer = document.querySelector('.reviews-section .row.justify-content-center');
    if (!reviewsContainer) return;

    const topReviews = getFeaturedReviews();
    reviewsContainer.innerHTML = topReviews.map(createFeaturedReviewHTML).join('');
}

// Xử lý click cho dropdown menu danh mục
document.addEventListener('DOMContentLoaded', function() {
    // Cập nhật số lượng sản phẩm trong danh mục
    updateCategoryProductCounts();
    
    // Cập nhật carousel sản phẩm bán chạy
    updateBestSellingProductsCarousel();
    
    // Cập nhật đánh giá nổi bật
    updateFeaturedReviews();

    // Dropdown menu trong navbar - chuyển trang ngay lập tức
    const dropdownItems = document.querySelectorAll('.navbar .dropdown-item[data-category]');
    
    dropdownItems.forEach(item => {
        item.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            const category = this.getAttribute('data-category');
            // Chuyển trang ngay lập tức
            window.location.href = `sanpham.html?category=${category}`;
        });
    });

    // Category cards trong danh mục nổi bật - đã có href sẵn trong HTML
    // Không cần thêm JavaScript vì đã có link trực tiếp
});
})