// Global variables
let revenueChart = null;
let categoryChart = null;
let originalChartData = {
    labels: [],
    data: []
};
let currentChartData = {
    labels: [],
    data: [],
    month: new Date().getMonth() + 1,
    year: new Date().getFullYear()
};

// Khởi tạo khi DOM đã load
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing dashboard...');
    console.log('Available globals:', { 
        Chart: typeof Chart, 
        XLSX: typeof XLSX,
        window: typeof window
    });
    
    // Kiểm tra dữ liệu từ server
    console.log('Revenue chart data check:');
    console.log('Labels:', window.doanhThu12ThangLabels);
    console.log('Data:', window.doanhThu12ThangData);
    
    // Lưu dữ liệu gốc
    originalChartData.labels = window.doanhThu12ThangLabels || [];
    originalChartData.data = window.doanhThu12ThangData || [];
    
    // Khởi tạo dữ liệu hiện tại với giá trị từ server
    currentChartData.labels = [...originalChartData.labels];
    currentChartData.data = [...originalChartData.data];
    currentChartData.month = window.currentMonth || new Date().getMonth() + 1;
    currentChartData.year = window.currentYear || new Date().getFullYear();
    
    console.log('Initialized currentChartData:', currentChartData);
    
    // Animation cho cards
    initializeCardAnimations();
    
    // Khởi tạo event listeners cho các nút (chỉ gọi một lần)
    initializeMonthNavigation();
    initializeExportButton();
    
    // Khởi tạo chart doanh thu với delay
    setTimeout(() => {
        initializeRevenueChart();
        initializeCategoryChart();
    }, 800);
    
    // Khởi tạo phân trang danh mục
    initializeDanhMucPagination();
});

// Animation cho cards
function initializeCardAnimations() {
    const cards = document.querySelectorAll('.fade-in-up');
    cards.forEach((card, index) => {
        setTimeout(() => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(30px)';
            card.style.transition = 'all 0.6s cubic-bezier(0.4, 0, 0.2, 1)';
            
            setTimeout(() => {
                card.style.opacity = '1';
                card.style.transform = 'translateY(0)';
            }, 50);
        }, index * 100);
    });
}

// Khởi tạo biểu đồ doanh thu
function initializeRevenueChart() {
    console.log('Initializing revenue chart...');
    
    const canvas = document.getElementById('revenueChart');
    if (!canvas) {
        console.error('Revenue chart canvas not found!');
        return;
    }
    
    // Kiểm tra Chart.js đã load chưa
    if (typeof Chart === 'undefined') {
        console.error('Chart.js library not loaded!');
        return;
    }
    
    // Kiểm tra dữ liệu
    const labels = window.doanhThu12ThangLabels || [];
    const data = window.doanhThu12ThangData || [];
    
    console.log('Chart data:', { labels, data });
    
    if (labels.length === 0 || data.length === 0) {
        console.warn('No data available for revenue chart');
        showNoDataMessage();
        return;
    }
    
    createRevenueChart(canvas, labels, data);
}

// Tạo biểu đồ doanh thu
function createRevenueChart(canvas, labels, data) {
    const ctx = canvas.getContext('2d');
    
    // Destroy existing chart if exists
    if (revenueChart) {
        revenueChart.destroy();
    }
    
    // Tạo gradient
    const gradient = ctx.createLinearGradient(0, 0, 0, 400);
    gradient.addColorStop(0, 'rgba(108, 92, 231, 0.3)');
    gradient.addColorStop(1, 'rgba(108, 92, 231, 0.05)');
    
    try {
        revenueChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Doanh thu (VNĐ)',
                    data: data,
                    borderColor: '#6c5ce7',
                    backgroundColor: gradient,
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#6c5ce7',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 3,
                    pointRadius: 6,
                    pointHoverRadius: 10,
                    pointHoverBackgroundColor: '#a29bfe',
                    pointHoverBorderColor: '#fff',
                    pointHoverBorderWidth: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0,0,0,0.8)',
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        cornerRadius: 10,
                        padding: 15,
                        displayColors: false,
                        callbacks: {
                            title: function(context) {
                                return `Ngày ${context[0].label}`;
                            },
                            label: function(context) {
                                return 'Doanh thu: ' + formatCurrency(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0,0,0,0.1)',
                            drawBorder: false
                        },
                        ticks: {
                            padding: 10,
                            font: {
                                size: 11
                            },
                            callback: function(value) {
                                return formatCurrencyCompact(value);
                            }
                        }
                    },
                    x: {
                        grid: {
                            color: 'rgba(0,0,0,0.05)',
                            drawBorder: false
                        },
                        ticks: {
                            padding: 10,
                            font: {
                                size: 11
                            }
                        }
                    }
                },
                elements: {
                    point: {
                        hoverRadius: 10
                    }
                },
                animation: {
                    duration: 2000,
                    easing: 'easeInOutQuart'
                },
                interaction: {
                    intersect: false,
                    mode: 'index'
                }
            }
        });
        
        console.log('Revenue chart created successfully!', revenueChart);
        
    } catch (error) {
        console.error('Error creating revenue chart:', error);
        showChartError();
    }
}

// Hiển thị thông báo không có dữ liệu
function showNoDataMessage() {
    const chartContainer = document.querySelector('#revenueChart').parentElement;
    const messageDiv = document.createElement('div');
    messageDiv.className = 'text-center py-5';
    messageDiv.innerHTML = `
        <i class="fas fa-chart-line fa-3x text-muted mb-3"></i>
        <h5 class="text-muted">Chưa có dữ liệu doanh thu</h5>
        <p class="text-muted">Dữ liệu sẽ được cập nhật khi có đơn hàng</p>
    `;
    chartContainer.appendChild(messageDiv);
}

// Hiển thị lỗi biểu đồ
function showChartError() {
    const chartContainer = document.querySelector('#revenueChart').parentElement;
    const errorDiv = document.createElement('div');
    errorDiv.className = 'text-center py-5';
    errorDiv.innerHTML = `
        <i class="fas fa-exclamation-triangle fa-3x text-warning mb-3"></i>
        <h5 class="text-warning">Lỗi hiển thị biểu đồ</h5>
        <p class="text-muted">Vui lòng tải lại trang</p>
    `;
    chartContainer.appendChild(errorDiv);
}

// Format tiền tệ
function formatCurrency(value) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(value);
}

// Format tiền tệ dạng compact
function formatCurrencyCompact(value) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        notation: 'compact'
    }).format(value);
}

// Phân trang danh mục
function initializeDanhMucPagination() {
    console.log('Initializing category pagination...');
    
    const categoryRows = document.querySelectorAll('.category-row');
    if (categoryRows.length === 0) {
        console.log('No category rows found');
        return;
    }
    
    // Hiển thị trang đầu tiên
    showDanhMucPage(1);
    
    // Lắng nghe sự kiện click cho các nút phân trang
    const paginationButtons = document.querySelectorAll('.page-category-btn');
    paginationButtons.forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            const page = parseInt(this.getAttribute('data-page'));
            if (!isNaN(page)) {
                showDanhMucPage(page);
            }
        });
    });
    
    console.log(`Category pagination initialized with ${categoryRows.length} rows`);
}

// Hiển thị trang danh mục
function showDanhMucPage(page) {
    const rows = document.querySelectorAll('.category-row');
    const pageSize = 5;
    
    // Ẩn/hiện các hàng theo trang
    rows.forEach((row, index) => {
        const rowPage = Math.floor(index / pageSize) + 1;
        if (rowPage === page) {
            row.classList.remove('d-none');
        } else {
            row.classList.add('d-none');
        }
    });
    
    // Cập nhật active state cho pagination
    document.querySelectorAll('.page-category-btn').forEach(btn => {
        btn.parentElement.classList.remove('active');
    });
    
    const activeBtn = document.querySelector(`.page-category-btn[data-page="${page}"]`);
    if (activeBtn) {
        activeBtn.parentElement.classList.add('active');
    }
    
    console.log(`Showing category page ${page}`);
}

// Utility functions
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Khởi tạo điều hướng tháng
function initializeMonthNavigation() {
    console.log('Initializing month navigation...');
    
    const prevBtn = document.getElementById('prevMonthBtn');
    const nextBtn = document.getElementById('nextMonthBtn');
    const currentBtn = document.getElementById('currentMonthBtn');
    
    console.log('Found buttons:', { prevBtn, nextBtn, currentBtn });
    
    if (prevBtn) {
        console.log('Adding event listener to prev button');
        prevBtn.addEventListener('click', (e) => {
            e.preventDefault();
            console.log('Prev button clicked');
            navigateMonth(-1);
        });
    } else {
        console.error('Prev button not found!');
    }
    
    if (nextBtn) {
        console.log('Adding event listener to next button');
        nextBtn.addEventListener('click', (e) => {
            e.preventDefault();
            console.log('Next button clicked');
            navigateMonth(1);
        });
    } else {
        console.error('Next button not found!');
    }
    
    if (currentBtn) {
        console.log('Adding event listener to current button');
        currentBtn.addEventListener('click', (e) => {
            e.preventDefault();
            console.log('Current button clicked');
            goToCurrentMonth();
        });
    } else {
        console.error('Current button not found!');
    }
}

// Điều hướng tháng
function navigateMonth(direction) {
    console.log('Navigating month, direction:', direction);
    console.log('Current month/year before:', currentChartData.month, currentChartData.year);
    
    currentChartData.month += direction;
    
    if (currentChartData.month > 12) {
        currentChartData.month = 1;
        currentChartData.year += 1;
    } else if (currentChartData.month < 1) {
        currentChartData.month = 12;
        currentChartData.year -= 1;
    }
    
    console.log('Current month/year after:', currentChartData.month, currentChartData.year);
    
    updateMonthDisplay();
    loadMonthData(currentChartData.month, currentChartData.year);
}

// Về tháng hiện tại
function goToCurrentMonth() {
    console.log('Going to current month...');
    
    currentChartData.month = window.currentMonth || new Date().getMonth() + 1;
    currentChartData.year = window.currentYear || new Date().getFullYear();
    
    console.log('Reset to current month/year:', currentChartData.month, currentChartData.year);
    
    // Sử dụng dữ liệu gốc
    currentChartData.labels = [...originalChartData.labels];
    currentChartData.data = [...originalChartData.data];
    
    updateMonthDisplay();
    updateChart(currentChartData.labels, currentChartData.data);
}

// Cập nhật hiển thị tháng
function updateMonthDisplay() {
    console.log('Updating month display...');
    
    const monthDisplay = document.getElementById('currentMonthDisplay');
    console.log('Month display element:', monthDisplay);
    
    if (monthDisplay) {
        const monthStr = currentChartData.month.toString().padStart(2, '0');
        const newText = `${monthStr}/${currentChartData.year}`;
        console.log('Setting month display to:', newText);
        monthDisplay.textContent = newText;
    } else {
        console.error('Month display element not found!');
    }
}

// Tải dữ liệu tháng
async function loadMonthData(month, year) {
    console.log(`Loading data for month ${month}/${year}...`);
    
    try {
        showLoading(true);
        
        const url = `/admin/baocao/getdoanhthutheothang?thang=${month}&nam=${year}`;
        console.log('Fetching from URL:', url);
        
        const response = await fetch(url);
        console.log('Response status:', response.status);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        console.log('Received data:', data);
        
        if (data.error) {
            throw new Error(data.error);
        }
        
        // Cập nhật dữ liệu hiện tại
        currentChartData.labels = data.labels || [];
        currentChartData.data = data.data || [];
        
        console.log('Updated currentChartData:', currentChartData);
        
        // Cập nhật biểu đồ
        updateChart(currentChartData.labels, currentChartData.data);
        
        // Force update after a short delay to ensure DOM is ready
        setTimeout(() => {
            updateChart(currentChartData.labels, currentChartData.data);
        }, 200);
        
        console.log('Month data loaded and chart updated successfully');
        
    } catch (error) {
        console.error('Error loading month data:', error);
        showError('Không thể tải dữ liệu tháng này: ' + error.message);
    } finally {
        showLoading(false);
    }
}

// Hiển thị/ẩn loading
function showLoading(show) {
    const loading = document.getElementById('chartLoading');
    const canvas = document.getElementById('revenueChart');
    
    if (show) {
        if (loading) loading.classList.remove('d-none');
        if (canvas) canvas.style.opacity = '0.3';
    } else {
        if (loading) loading.classList.add('d-none');
        if (canvas) canvas.style.opacity = '1';
    }
}

// Tạo lại biểu đồ hoàn toàn
function recreateChart(labels, data) {
    console.log('Recreating chart with new data:', { labels, data });
    
    const canvas = document.getElementById('revenueChart');
    if (!canvas) {
        console.error('Canvas not found for recreation');
        return;
    }
    
    // Destroy existing chart
    if (revenueChart) {
        revenueChart.destroy();
        revenueChart = null;
    }
    
    // Clear canvas
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    // Recreate chart
    createRevenueChart(canvas, labels, data);
    
    console.log('Chart recreated successfully');
}

// Cập nhật biểu đồ
function updateChart(labels, data) {
    console.log('Updating chart with:', { labels, data });
    
    if (!revenueChart) {
        console.error('Revenue chart not initialized');
        // Try to recreate if chart is null
        const canvas = document.getElementById('revenueChart');
        if (canvas) {
            createRevenueChart(canvas, labels, data);
            return;
        }
        return;
    }
    
    console.log('Current chart:', revenueChart);
    console.log('Current chart data before update:', {
        labels: revenueChart.data.labels,
        data: revenueChart.data.datasets[0].data
    });
    
    try {
        // Clear previous data first
        revenueChart.data.labels.length = 0;
        revenueChart.data.datasets[0].data.length = 0;
        
        // Add new data
        revenueChart.data.labels.push(...labels);
        revenueChart.data.datasets[0].data.push(...data);
        
        console.log('Chart data after update:', {
            labels: revenueChart.data.labels,
            data: revenueChart.data.datasets[0].data
        });
        
        // Force update with animation
        revenueChart.update('active');
        
        // Additional force update without animation as backup
        setTimeout(() => {
            revenueChart.update('none');
            console.log('Secondary update completed');
        }, 100);
        
        console.log('Chart updated successfully with method 1');
    } catch (error) {
        console.warn('Method 1 failed, trying method 2 (recreation):', error);
        
        // Method 2: Complete recreation
        try {
            recreateChart(labels, data);
            console.log('Chart recreated successfully with method 2');
        } catch (error2) {
            console.error('Both methods failed:', error2);
        }
    }
}

// Khởi tạo nút xuất Excel
function initializeExportButton() {
    console.log('Initializing export button...');
    
    const exportBtn = document.getElementById('exportExcelBtn');
    console.log('Found export button:', exportBtn);
    
    if (exportBtn) {
        console.log('Adding event listener to export button');
        exportBtn.addEventListener('click', (e) => {
            e.preventDefault();
            console.log('Export button clicked');
            exportToExcel();
        });
    } else {
        console.error('Export button not found!');
    }
}

// Xuất dữ liệu ra Excel
function exportToExcel() {
    console.log('Starting export to Excel...');
    
    // Kiểm tra thư viện XLSX
    if (typeof XLSX === 'undefined') {
        console.error('XLSX library not loaded!');
        showError('Thư viện Excel chưa được tải. Vui lòng tải lại trang.');
        return;
    }
    
    // Kiểm tra dữ liệu
    if (!currentChartData.labels || currentChartData.labels.length === 0) {
        console.warn('No chart data available for export');
        showError('Không có dữ liệu để xuất');
        return;
    }
    
    try {
        console.log('Preparing data for export...');
        console.log('Current chart data:', currentChartData);
        
        // Chuẩn bị dữ liệu
        const data = [];
        data.push(['Ngày', 'Doanh thu (VNĐ)']);
        
        for (let i = 0; i < currentChartData.labels.length; i++) {
            const label = currentChartData.labels[i];
            const value = currentChartData.data[i] || 0;
            data.push([
                `Ngày ${label}`,
                Number(value)
            ]);
        }
        
        console.log('Data prepared:', data);
        
        // Tạo workbook
        const ws = XLSX.utils.aoa_to_sheet(data);
        const wb = XLSX.utils.book_new();
        
        // Định dạng cột
        if (ws['!ref']) {
            const range = XLSX.utils.decode_range(ws['!ref']);
            for (let row = 1; row <= range.e.r; row++) {
                const cellRef = XLSX.utils.encode_cell({ r: row, c: 1 });
                if (ws[cellRef]) {
                    ws[cellRef].t = 'n';
                    ws[cellRef].z = '#,##0';
                }
            }
        }
        
        // Thêm worksheet vào workbook
        const monthStr = currentChartData.month.toString().padStart(2, '0');
        const sheetName = `Doanh thu ${monthStr}-${currentChartData.year}`;
        XLSX.utils.book_append_sheet(wb, ws, sheetName);
        
        // Xuất file
        const fileName = `doanh-thu-${monthStr}-${currentChartData.year}.xlsx`;
        XLSX.writeFile(wb, fileName);
        
        // Hiển thị thông báo thành công
        showSuccessMessage('Xuất Excel thành công!');
        
    } catch (error) {
        console.error('Error exporting to Excel:', error);
        showError('Không thể xuất file Excel: ' + error.message);
    }
}

// Hiển thị thông báo lỗi
function showError(message) {
    // Có thể tích hợp với toast notification library
    alert('Lỗi: ' + message);
}

// Hiển thị thông báo thành công
function showSuccessMessage(message) {
    // Có thể tích hợp với toast notification library
    console.log('Success: ' + message);
    
    // Tạm thời sử dụng một thông báo đơn giản
    const alertDiv = document.createElement('div');
    alertDiv.className = 'alert alert-success alert-dismissible fade show position-fixed';
    alertDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    alertDiv.innerHTML = `
        <i class="fas fa-check-circle mr-2"></i>
        ${message}
        <button type="button" class="close" data-dismiss="alert">
            <span>&times;</span>
        </button>
    `;
    
    document.body.appendChild(alertDiv);
    
    // Tự động ẩn sau 3 giây
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.parentNode.removeChild(alertDiv);
        }
    }, 3000);
}

// Error handling
window.addEventListener('error', function(e) {
    console.error('JavaScript error:', e.error);
});

// Log khi script được load
console.log('Baocao dashboard script loaded successfully');

// Test function để gọi thủ công từ console
window.testButtons = function() {
    console.log('Testing buttons manually...');
    const prevBtn = document.getElementById('prevMonthBtn');
    const nextBtn = document.getElementById('nextMonthBtn');
    const currentBtn = document.getElementById('currentMonthBtn');
    const exportBtn = document.getElementById('exportExcelBtn');
    
    console.log('Button elements:', { prevBtn, nextBtn, currentBtn, exportBtn });
    
    if (prevBtn) {
        prevBtn.click();
        console.log('Prev button clicked');
    }
};

window.testAPI = async function() {
    console.log('Testing API manually...');
    try {
        const response = await fetch('/admin/baocao/getdoanhthutheothang?thang=8&nam=2025');
        const data = await response.json();
        console.log('API Response:', data);
    } catch (error) {
        console.error('API Error:', error);
    }
};

// Test function để thay đổi tháng thủ công
window.testMonthChange = async function(month, year) {
    console.log(`Testing month change to ${month}/${year}...`);
    currentChartData.month = month;
    currentChartData.year = year;
    updateMonthDisplay();
    await loadMonthData(month, year);
};

// Test function để cập nhật chart với dữ liệu test
window.testChartUpdate = function() {
    console.log('Testing chart update with sample data...');
    const testLabels = ['1', '2', '3', '4', '5'];
    const testData = [100000, 200000, 150000, 300000, 250000];
    updateChart(testLabels, testData);
};

// Manual test commands for console
console.log('Available test commands:');
console.log('- window.testMonthChange(7, 2025) // Test change to July 2025');
console.log('- window.testChartUpdate() // Test chart update with sample data');
console.log('- window.testAPI() // Test API call');
console.log('- window.checkChartStatus() // Check chart status');

// Check chart status
window.checkChartStatus = function() {
    console.log('Chart status check:');
    console.log('- revenueChart exists:', !!revenueChart);
    console.log('- revenueChart data:', revenueChart ? revenueChart.data : 'N/A');
    console.log('- categoryChart exists:', !!categoryChart);
    console.log('- categoryChart data:', categoryChart ? categoryChart.data : 'N/A');
    console.log('- currentChartData:', currentChartData);
    console.log('- revenue canvas element:', document.getElementById('revenueChart'));
    console.log('- category canvas element:', document.getElementById('categoryChart'));
};

// Khởi tạo biểu đồ danh mục
function initializeCategoryChart() {
    console.log('Initializing category chart...');
    
    const canvas = document.getElementById('categoryChart');
    if (!canvas) {
        console.error('Category chart canvas not found!');
        return;
    }
    
    // Kiểm tra Chart.js đã load chưa
    if (typeof Chart === 'undefined') {
        console.error('Chart.js library not loaded!');
        return;
    }
    
    // Kiểm tra dữ liệu
    const labels = window.categoryLabels || [];
    const data = window.categoryData || [];
    const colors = window.categoryColors || [];
    
    console.log('Category chart data:', { labels, data, colors });
    
    if (labels.length === 0 || data.length === 0) {
        console.warn('No data available for category chart');
        showNoCategoryDataMessage();
        return;
    }
    
    createCategoryChart(canvas, labels, data, colors);
}

// Tạo biểu đồ danh mục
function createCategoryChart(canvas, labels, data, colors) {
    const ctx = canvas.getContext('2d');
    
    // Destroy existing chart if exists
    if (categoryChart) {
        categoryChart.destroy();
    }
    
    try {
        categoryChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors,
                    borderColor: colors.map(color => color),
                    borderWidth: 2,
                    hoverBackgroundColor: colors.map(color => color + 'CC'),
                    hoverBorderColor: '#fff',
                    hoverBorderWidth: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '60%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            usePointStyle: true,
                            pointStyle: 'circle',
                            font: {
                                size: 12
                            },
                            generateLabels: function(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    return data.labels.map((label, i) => {
                                        const value = data.datasets[0].data[i];
                                        const total = data.datasets[0].data.reduce((a, b) => a + b, 0);
                                        const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                        
                                        return {
                                            text: `${label} (${percentage}%)`,
                                            fillStyle: data.datasets[0].backgroundColor[i],
                                            strokeStyle: data.datasets[0].borderColor[i],
                                            lineWidth: data.datasets[0].borderWidth,
                                            pointStyle: 'circle',
                                            hidden: false,
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0,0,0,0.8)',
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        cornerRadius: 10,
                        padding: 15,
                        displayColors: true,
                        callbacks: {
                            title: function(context) {
                                return context[0].label;
                            },
                            label: function(context) {
                                const value = context.parsed;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                return [
                                    `Số lượng: ${value.toLocaleString('vi-VN')}`,
                                    `Tỷ lệ: ${percentage}%`
                                ];
                            }
                        }
                    }
                },
                animation: {
                    animateScale: true,
                    animateRotate: true,
                    duration: 2000,
                    easing: 'easeOutQuart'
                },
                onHover: (event, activeElements) => {
                    event.native.target.style.cursor = activeElements.length > 0 ? 'pointer' : 'default';
                }
            }
        });
        
        console.log('Category chart created successfully');
        
    } catch (error) {
        console.error('Error creating category chart:', error);
        showCategoryChartError();
    }
}

// Hiển thị thông báo không có dữ liệu cho biểu đồ danh mục
function showNoCategoryDataMessage() {
    const canvas = document.getElementById('categoryChart');
    if (canvas) {
        const container = canvas.parentElement;
        container.innerHTML = `
            <div class="d-flex align-items-center justify-content-center h-100">
                <div class="text-center text-muted">
                    <i class="fas fa-chart-pie fa-3x mb-3 opacity-50"></i>
                    <h5>Chưa có dữ liệu</h5>
                    <p class="mb-0">Không có dữ liệu danh mục để hiển thị</p>
                </div>
            </div>
        `;
    }
}

// Hiển thị thông báo lỗi cho biểu đồ danh mục
function showCategoryChartError() {
    const canvas = document.getElementById('categoryChart');
    if (canvas) {
        const container = canvas.parentElement;
        container.innerHTML = `
            <div class="d-flex align-items-center justify-content-center h-100">
                <div class="text-center text-danger">
                    <i class="fas fa-exclamation-triangle fa-3x mb-3"></i>
                    <h5>Lỗi tải biểu đồ</h5>
                    <p class="mb-0">Không thể tải biểu đồ danh mục</p>
                </div>
            </div>
        `;
    }
}
