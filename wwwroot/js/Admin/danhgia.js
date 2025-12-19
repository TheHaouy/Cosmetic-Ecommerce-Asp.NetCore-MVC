// Quản lý đánh giá - Admin JavaScript

$(document).ready(function () {
    // Select all checkbox
    $('#selectAll').on('change', function () {
        $('.review-checkbox').prop('checked', $(this).prop('checked'));
        toggleDeleteMultipleBtn();
    });

    // Individual checkbox
    $('.review-checkbox').on('change', function () {
        const allChecked = $('.review-checkbox').length === $('.review-checkbox:checked').length;
        $('#selectAll').prop('checked', allChecked);
        toggleDeleteMultipleBtn();
    });

    // Toggle delete multiple button
    function toggleDeleteMultipleBtn() {
        const checkedCount = $('.review-checkbox:checked').length;
        if (checkedCount > 0) {
            $('#deleteMultipleBtn').show().text(`Xóa đã chọn (${checkedCount})`);
        } else {
            $('#deleteMultipleBtn').hide();
        }
    }

    // Toggle detail row
    $('.btn-toggle-detail').on('click', function () {
        const reviewId = $(this).data('id');
        const hasReply = $(this).data('has-reply');
        const $detailRow = $(`#detail-${reviewId}`);
        const $icon = $(this).find('i');
        const $row = $(this).closest('tr');
        
        // Đóng tất cả các detail rows khác
        $('.review-detail-row').not($detailRow).slideUp(300);
        $('.btn-toggle-detail').not(this).find('i').removeClass('fa-chevron-up').addClass('fa-chevron-down');
        $('.review-row').not($row).removeClass('active');
        
        // Toggle row hiện tại
        if ($detailRow.is(':visible')) {
            $detailRow.slideUp(300);
            $icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
            $row.removeClass('active');
        } else {
            // Load data nếu chưa có hoặc hiển thị nếu đã có
            if ($detailRow.find('.review-detail-container').children('.loading-indicator').length > 0) {
                // Convert hasReply to boolean - nếu đã có reply thì không hiển thị form
                const showReplyForm = !(hasReply === true || hasReply === 'True' || hasReply === 'true');
                loadReviewDetail(reviewId, showReplyForm);
            }
            $detailRow.slideDown(300);
            $icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
            $row.addClass('active');
        }
    });

    // Submit reply inline
    $(document).on('click', '.btn-submit-reply-inline', function () {
        const reviewId = $(this).data('id');
        const $textarea = $(`#replyText-${reviewId}`);
        const replyText = $textarea.val().trim();

        if (!replyText) {
            Swal.fire({
                icon: 'warning',
                title: 'Cảnh báo',
                text: 'Vui lòng nhập nội dung trả lời',
                confirmButtonText: 'OK'
            });
            return;
        }

        const $btn = $(this);
        const originalText = $btn.html();
        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Đang gửi...');

        $.ajax({
            url: '/admin/danhgia/replyreview',
            type: 'POST',
            data: {
                idDanhGia: reviewId,
                traLoi: replyText,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Thành công',
                        text: response.message,
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Lỗi',
                        text: response.message,
                        confirmButtonText: 'OK'
                    });
                }
            },
            error: function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi',
                    text: 'Có lỗi xảy ra khi gửi phản hồi',
                    confirmButtonText: 'OK'
                });
            },
            complete: function () {
                $btn.prop('disabled', false).html(originalText);
            }
        });
    });

    // Cancel reply inline
    $(document).on('click', '.btn-cancel-reply-inline', function () {
        const reviewId = $(this).data('id');
        $(`#replyText-${reviewId}`).val('');
    });

    // Delete single review
    $('.btn-delete').on('click', function () {
        const reviewId = $(this).data('id');
        confirmDelete([reviewId]);
    });

    // Delete multiple reviews
    $('#deleteMultipleBtn').on('click', function () {
        const selectedIds = $('.review-checkbox:checked').map(function () {
            return parseInt($(this).val());
        }).get();

        if (selectedIds.length === 0) {
            Swal.fire({
                icon: 'warning',
                title: 'Cảnh báo',
                text: 'Vui lòng chọn ít nhất một đánh giá để xóa',
                confirmButtonText: 'OK'
            });
            return;
        }

        confirmDelete(selectedIds);
    });

    // Confirm delete function
    function confirmDelete(ids) {
        const isMultiple = ids.length > 1;
        const message = isMultiple
            ? `Bạn có chắc chắn muốn xóa ${ids.length} đánh giá đã chọn?`
            : 'Bạn có chắc chắn muốn xóa đánh giá này?';

        Swal.fire({
            title: 'Xác nhận xóa',
            text: message,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Xóa',
            cancelButtonText: 'Hủy'
        }).then((result) => {
            if (result.isConfirmed) {
                deleteReviews(ids);
            }
        });
    }

    // Delete reviews
    function deleteReviews(ids) {
        const url = ids.length === 1
            ? '/admin/danhgia/deletereview'
            : '/admin/danhgia/deletemultiple';

        const data = ids.length === 1
            ? { id: ids[0], __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val() }
            : { ids: ids, __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val() };

        $.ajax({
            url: url,
            type: 'POST',
            data: data,
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Thành công',
                        text: response.message,
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Lỗi',
                        text: response.message,
                        confirmButtonText: 'OK'
                    });
                }
            },
            error: function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi',
                    text: 'Có lỗi xảy ra khi xóa đánh giá',
                    confirmButtonText: 'OK'
                });
            }
        });
    }

    // Load review detail
    function loadReviewDetail(reviewId, showReplyForm) {
        const $container = $(`#detail-${reviewId} .review-detail-container`);
        
        $.ajax({
            url: '/admin/danhgia/getreviewdetail',
            type: 'GET',
            data: { id: reviewId },
            success: function (response) {
                if (response.success) {
                    const data = response.data;
                    const initials = getInitials(data.tenKhachHang);
                    const reviewerName = data.tenKhachHang || 'Khách ẩn danh';
                    const orderUrl = data.orderId ? `/admin/donhang/detail/${data.orderId}` : null;

                    const replyBlock = data.traLoiCuaShop ? `
                        <div class="admin-reply mt-4">
                            <div class="admin-reply-header">
                                <div class="admin-avatar">
                                    <i class="fas fa-store"></i>
                                </div>
                                <div class="admin-info">
                                    <span class="admin-name">Little Fish Beauty Shop</span>
                                    ${data.ngayTraLoi ? `<span class="reply-date">${data.ngayTraLoi}</span>` : ''}
                                </div>
                            </div>
                            <div class="admin-reply-content">
                                <p>${data.traLoiCuaShop}</p>
                            </div>
                        </div>
                    ` : (showReplyForm ? `
                        <div class="reply-form-section mt-4">
                            <div class="reply-form-header">
                                <i class="fas fa-reply me-2"></i>
                                <strong>Trả lời đánh giá này</strong>
                            </div>
                            <div class="reply-form-body">
                                <textarea class="form-control" id="replyText-${reviewId}" rows="4" placeholder="Nhập phản hồi của bạn cho khách hàng..."></textarea>
                                <div class="reply-form-actions mt-3">
                                    <button type="button" class="btn btn-secondary btn-cancel-reply-inline" data-id="${reviewId}">
                                        <i class="fas fa-times me-2"></i>Hủy
                                    </button>
                                    <button type="button" class="btn btn-primary btn-submit-reply-inline" data-id="${reviewId}">
                                        <i class="fas fa-paper-plane me-2"></i>Gửi phản hồi
                                    </button>
                                </div>
                            </div>
                        </div>
                    ` : `
                        <div class="alert alert-info mt-4 mb-0">
                            <i class="fas fa-info-circle me-2"></i>
                            Chưa có phản hồi từ Shop
                        </div>
                    `);

                    let html = `
                        <div class="review-detail-content-wrapper">
                            <div class="review-item">
                                <div class="review-header">
                                    <div class="reviewer-avatar">${initials}</div>
                                    <div class="reviewer-info">
                                        <div class="reviewer-name">${reviewerName}</div>
                                        <div class="review-date text-muted">
                                            <i class="far fa-calendar-alt me-1"></i>${data.ngayDanhGia || ''}
                                        </div>
                                        ${data.tenSanPham ? `<div class="review-order-label text-muted small">
                                            <i class="fas fa-box me-1"></i>${data.tenSanPham}
                                        </div>` : ''}
                                    </div>
                                    <div class="review-stars">
                                        ${generateStars(data.soSao)}
                                    </div>
                                </div>
                                ${orderUrl ? `
                                    <div class="review-order-actions">
                                        <button type="button" class="btn btn-outline-primary btn-sm" onclick="window.open('${orderUrl}', '_blank')">
                                            <i class="fas fa-receipt me-1"></i>Đơn #${data.orderId}
                                        </button>
                                        <span class="text-muted small ms-2">
                                            <i class="far fa-clock me-1"></i>${data.orderDate || ''}
                                        </span>
                                        ${data.orderStatus ? `<span class="badge bg-info ms-2">${data.orderStatus}</span>` : ''}
                                    </div>
                                ` : ''}
                                <div class="review-content">
                                    <p class="review-text">
                                        ${data.binhLuan || '<em class="text-muted">Không có nhận xét</em>'}
                                    </p>
                                    ${data.anhDanhGia ? `
                                        <div class="review-images">
                                            ${generateReviewImages(data.anhDanhGia, 'detail')}
                                        </div>
                                    ` : ''}
                                </div>
                                ${replyBlock}
                            </div>
                        </div>
                    `;

                    $container.html(html);
                } else {
                    $container.html(`
                        <div class="alert alert-danger mb-0">
                            <i class="fas fa-exclamation-triangle me-2"></i>${response.message}
                        </div>
                    `);
                }
            },
            error: function () {
                $container.html(`
                    <div class="alert alert-danger mb-0">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        Có lỗi xảy ra khi tải chi tiết đánh giá
                    </div>
                `);
            }
        });
    }

    // Helper functions
    function generateStars(rating) {
        let stars = '';
        for (let i = 1; i <= 5; i++) {
            if (i <= rating) {
                stars += '<i class="fas fa-star text-warning"></i> ';
            } else {
                stars += '<i class="far fa-star text-muted"></i> ';
            }
        }
        return stars;
    }

    function getInitials(name) {
        if (!name) return '?';
        const parts = name.trim().split(' ');
        if (parts.length >= 2) {
            return parts[0][0].toUpperCase() + parts[parts.length - 1][0].toUpperCase();
        }
        return name[0].toUpperCase();
    }

    function generateReviewImages(anhDanhGia, mode = 'grid') {
        if (!anhDanhGia) return '';

        const images = anhDanhGia.split(/[,;]/).map(img => img.trim()).filter(img => img);

        return images.map(img => `
            <div class="review-image-item ${mode === 'detail' ? 'review-image-detail' : ''}">
                <img src="${img}" alt="Review image" class="review-image-thumb" 
                     onerror="this.src='/images/noimage.jpg'">
            </div>
        `).join('');
    }

    // Click to open modal for enlarged image
    $(document).on('click', '.review-image-thumb', function () {
        const src = $(this).attr('src');
        $('#reviewImageModalImg').attr('src', src);
        const modalEl = document.getElementById('reviewImageModal');
        if (modalEl) {
            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        }
    });
});
