$(document).ready(function() {
    // Search functionality (fuzzy, case-insensitive)
    $('#searchInput').on('keyup', function() {
        var value = $(this).val().toLowerCase();
        $('#orderTable tbody tr').each(function() {
            var rowText = $(this).text().toLowerCase();
            if (fuzzyMatch(rowText, value)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });

    // Simple fuzzy match: returns true if all characters in pattern appear in order in text (not necessarily consecutively)
    function fuzzyMatch(text, pattern) {
        if (!pattern) return true;
        var t = 0, p = 0;
        while (t < text.length && p < pattern.length) {
            if (text[t] === pattern[p]) p++;
            t++;
        }
        return p === pattern.length;
    }

    // Status filter
    $('#statusFilter').change(function() {
        var selectedStatus = $(this).val().toLowerCase();
        $('#orderTable tbody tr').each(function() {
            var status = $(this).find('td:eq(4)').text().toLowerCase();
            if (selectedStatus === '' || status.indexOf(selectedStatus) > -1) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });

    // Click on row to view order detail
    $('.order-row').click(function() {
        var orderId = $(this).data('order-id');
        window.location.href = '/admin/donhang/detail/' + orderId;
    });

    // Add hover effect for better UX
    $('.order-row').hover(
        function() {
            $(this).addClass('table-active');
        },
        function() {
            $(this).removeClass('table-active');
        }
    );

    // Click on status badge to cycle through allowed statuses
    $('.status-badge').click(function(e) {
        e.stopPropagation(); // Prevent row click
        var orderId = $(this).data('order-id');
        var currentStatus = $(this).text().trim();

        var allowed = ['Chờ xác nhận', 'Đã xác nhận', 'Đang giao'];
        if (!allowed.includes(currentStatus)) {
            return; // Only these statuses can be toggled
        }

        var nextStatus = getNextStatus(currentStatus);
        
        if (nextStatus) {
            updateOrderStatus(orderId, nextStatus);
        }
    });

    // Click on timeline button to view full timeline
    $('.view-timeline-btn').click(function(e) {
        e.stopPropagation(); // Prevent row click
        var orderId = $(this).data('order-id');
        loadTimeline(orderId);
    });
});

function loadOrderDetail(orderId) {
    // Show loading state
    $('#orderModalId').text(orderId);
    $('#orderCustomerName').text('Đang tải...');
    $('#orderCustomerEmail').text('Đang tải...');
    $('#orderCustomerPhone').text('Đang tải...');
    $('#orderAddress').text('Đang tải...');
    $('#orderDate').text('Đang tải...');
    $('#orderPaymentMethod').text('Đang tải...');
    $('#orderTotal').text('Đang tải...');
    
    var itemsList = $('#orderItemsList');
    itemsList.html('<tr><td colspan="5" class="text-center">Đang tải chi tiết sản phẩm...</td></tr>');
    
    $('#orderDetailModal').modal('show');
    
    // Load order details from server
    $.ajax({
        url: window.donhangUrls.getOrderDetail,
        type: 'GET',
        data: { id: orderId },
        success: function(data) {
            // Update modal content
            $('#orderModalId').text(data.id);
            $('#orderCustomerName').text(data.customerName);
            $('#orderCustomerEmail').text(data.customerEmail);
            $('#orderCustomerPhone').text(data.customerPhone);
            $('#orderAddress').text(data.address);
            $('#orderDate').text(data.orderDate);
            $('#orderPaymentMethod').text(data.paymentMethod);
            $('#orderTotal').text(data.totalAmount);
            
            // Update status badge
            var statusBadge = $('#orderStatus');
            statusBadge.removeClass().addClass('badge ' + getStatusClass(data.status));
            statusBadge.text(data.status);
            
            // Update items list
            var itemsHtml = '';
            if (data.items && data.items.length > 0) {
                data.items.forEach(function(item) {
                    itemsHtml += '<tr>' +
                        '<td>' + item.productName + '</td>' +
                        '<td>' + item.variantSku + '</td>' +
                        '<td>' + item.quantity + '</td>' +
                        '<td>' + item.price + '</td>' +
                        '<td>' + item.subTotal + '</td>' +
                        '</tr>';
                });
            } else {
                itemsHtml = '<tr><td colspan="5" class="text-center">Không có sản phẩm nào</td></tr>';
            }
            itemsList.html(itemsHtml);
        },
        error: function() {
            alert('Không thể tải thông tin đơn hàng');
        }
    });
}

function getNextStatus(currentStatus) {
    // Forward-only progression
    var statuses = [
        'Chờ xác nhận',
        'Đã xác nhận',
        'Đang giao',
        'Hoàn thành'
    ];

    var currentIndex = statuses.indexOf(currentStatus);
    if (currentIndex === -1 || currentIndex === statuses.length - 1) return null; // no next or invalid

    var nextIndex = currentIndex + 1;
    return statuses[nextIndex];
}

function updateOrderStatus(orderId, status) {
    $.ajax({
        url: window.donhangUrls.updateStatus,
        type: 'POST',
        data: { id: orderId, status: status },
        success: function(response) {
            if (response.success) {
                // Update the status badge immediately without page reload
                var badge = $('.status-badge[data-order-id="' + orderId + '"]');
                badge.removeClass().addClass('badge status-badge ' + getStatusClass(status));
                badge.text(status);
                badge.attr('title', 'Click để thay đổi trạng thái');
                badge.css('cursor', 'pointer');
                
                // Update timeline cell - Add new timeline entry
                var timelineCell = $('.timeline-cell[data-order-id="' + orderId + '"]');
                if (response.updateTime) {
                    var wrapper = timelineCell.find('.timeline-wrapper');
                    
                    // Determine status badge class
                    var statusBadgeClass = getStatusClass(status).replace('text-white', '');
                    if (status === 'Đang xử lý') statusBadgeClass = 'badge bg-warning text-dark';
                    else if (status === 'Chờ xác nhận') statusBadgeClass = 'badge bg-info text-dark';
                    else statusBadgeClass = 'badge ' + statusBadgeClass;
                    
                    if (wrapper.length > 0) {
                        // Update timeline summary (latest)
                        var summary = wrapper.find('.timeline-summary');
                        summary.find('.text-muted').html('<i class="fas fa-clock"></i> ' + response.updateTime);
                        summary.find('.badge').attr('class', statusBadgeClass).text(status);
                        
                        // Update count
                        var currentCountText = summary.find('small.text-muted.ms-2');
                        if (currentCountText.length > 0) {
                            var match = currentCountText.text().match(/\+(\d+)/);
                            if (match) {
                                var newCount = parseInt(match[1]) + 1;
                                currentCountText.html('<i class="fas fa-history"></i> +' + newCount + ' lịch sử');
                            }
                        } else {
                            // Thêm count mới
                            summary.find('.badge').parent().append(
                                '<small class="text-muted ms-2">' +
                                '<i class="fas fa-history"></i> +1 lịch sử' +
                                '</small>'
                            );
                        }
                        
                        // Add to timeline list at the top
                        var timelineList = wrapper.find('.timeline-list');
                        var newTimelineEntry = 
                            '<div class="timeline-entry mb-2 pb-2 border-bottom">' +
                            '<div class="d-flex align-items-center gap-2 mb-1">' +
                            '<i class="fas fa-circle text-primary" style="font-size: 6px;"></i>' +
                            '<small class="text-muted">' +
                            '<i class="fas fa-clock"></i> ' + response.updateTime +
                            '</small>' +
                            '</div>' +
                            '<div class="ps-3">' +
                            '<span class="' + statusBadgeClass + '">' + status + '</span>' +
                            '<div class="timeline-note">' +
                            '<small class="text-muted fst-italic">Vừa cập nhật trạng thái</small>' +
                            '</div>' +
                            '</div>' +
                            '</div>';
                        
                        // Insert after the <hr> tag
                        var hr = timelineList.find('hr');
                        if (hr.length > 0) {
                            hr.after(newTimelineEntry);
                        } else {
                            timelineList.prepend(newTimelineEntry);
                        }
                    } else {
                        // Create new timeline wrapper (first timeline)
                        timelineCell.html(
                            '<div class="timeline-wrapper" data-order-id="' + orderId + '">' +
                            '<div class="timeline-summary" onclick="toggleTimeline(' + orderId + ', event)">' +
                            '<div class="d-flex align-items-center gap-2 mb-1">' +
                            '<i class="fas fa-circle text-primary" style="font-size: 6px;"></i>' +
                            '<small class="text-muted">' +
                            '<i class="fas fa-clock"></i> ' + response.updateTime +
                            '</small>' +
                            '</div>' +
                            '<div class="ps-3 d-flex align-items-center justify-content-between">' +
                            '<div>' +
                            '<span class="' + statusBadgeClass + '">' + status + '</span>' +
                            '</div>' +
                            '<i class="fas fa-chevron-down timeline-toggle-icon" style="font-size: 12px;"></i>' +
                            '</div>' +
                            '</div>' +
                            '<div class="timeline-list" style="display: none;">' +
                            '<hr class="my-2">' +
                            '<div class="timeline-entry mb-2 pb-2 border-bottom">' +
                            '<div class="d-flex align-items-center gap-2 mb-1">' +
                            '<i class="fas fa-circle text-primary" style="font-size: 6px;"></i>' +
                            '<small class="text-muted">' +
                            '<i class="fas fa-clock"></i> ' + response.updateTime +
                            '</small>' +
                            '</div>' +
                            '<div class="ps-3">' +
                            '<span class="' + statusBadgeClass + '">' + status + '</span>' +
                            '<div class="timeline-note">' +
                            '<small class="text-muted fst-italic">Timeline đầu tiên</small>' +
                            '</div>' +
                            '</div>' +
                            '</div>' +
                            '</div>' +
                            '</div>'
                        );
                    }
                }
                
                // Show success message briefly
                var originalText = badge.text();
                badge.text('✓ Đã cập nhật');
                setTimeout(function() {
                    badge.text(originalText);
                }, 1000);
            } else {
                alert('Có lỗi xảy ra: ' + response.message);
            }
        },
        error: function() {
            alert('Có lỗi xảy ra khi cập nhật trạng thái');
        }
    });
}

function getStatusClass(status) {
    switch(status) {
        case 'Chờ xác nhận': return 'bg-warning text-dark';
        case 'Đã xác nhận': return 'bg-primary text-white';
        case 'Đang giao': return 'bg-info text-dark';
        case 'Hoàn thành': return 'bg-success text-dark';
        case 'Đã hủy': return 'bg-dark text-white';
        default: return 'bg-secondary text-white';
    }
}

function loadTimeline(orderId) {
    // Set order ID in modal
    $('#timelineOrderId').text(orderId);
    
    // Show loading state
    $('#timelineContent').html(
        '<div class="text-center py-4">' +
        '<div class="spinner-border text-primary" role="status">' +
        '<span class="visually-hidden">Đang tải...</span>' +
        '</div>' +
        '</div>'
    );
    
    // Show modal
    $('#timelineModal').modal('show');
    
    // Load timeline data from server
    $.ajax({
        url: window.donhangUrls.getTimeline,
        type: 'GET',
        data: { id: orderId },
        success: function(data) {
            if (data && data.length > 0) {
                var timelineHtml = '';
                data.forEach(function(item, index) {
                    var iconClass = getTimelineIconClass(item.status);
                    var statusBadgeClass = getStatusClass(item.status);
                    
                    timelineHtml += 
                        '<div class="timeline-item">' +
                        '<div class="timeline-icon ' + iconClass + '"></div>' +
                        '<div class="timeline-content">' +
                        '<div class="timeline-date">' +
                        '<i class="fas fa-calendar-alt"></i> ' + item.date +
                        '</div>' +
                        '<div class="timeline-status">' +
                        '<span class="badge ' + statusBadgeClass + '">' + item.status + '</span>' +
                        '</div>' +
                        (item.note ? '<div class="timeline-note">' + item.note + '</div>' : '') +
                        '</div>' +
                        '</div>';
                });
                $('#timelineContent').html(timelineHtml);
            } else {
                $('#timelineContent').html(
                    '<div class="text-center py-4 text-muted">' +
                    '<i class="fas fa-inbox fa-3x mb-3"></i>' +
                    '<p>Chưa có lịch sử thay đổi trạng thái</p>' +
                    '</div>'
                );
            }
        },
        error: function() {
            $('#timelineContent').html(
                '<div class="text-center py-4 text-danger">' +
                '<i class="fas fa-exclamation-triangle fa-3x mb-3"></i>' +
                '<p>Không thể tải lịch sử timeline</p>' +
                '</div>'
            );
        }
    });
}

function getTimelineIconClass(status) {
    switch(status) {
        case 'Chờ xác nhận':
        case 'Mới':
            return 'status-new';
        case 'Đang xử lý':
            return 'status-processing';
        case 'Đang giao':
            return 'status-shipping';
        case 'Hoàn thành':
            return 'status-completed';
        case 'Đã hủy':
            return 'status-cancelled';
        default:
            return 'status-new';
    }
}
