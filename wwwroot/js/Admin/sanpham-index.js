// Quản lý sản phẩm - JavaScript
$(document).ready(function() {
    // Khởi tạo Bootstrap tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl, {
            trigger: 'hover',
            delay: { show: 300, hide: 100 }
        });
    });
    
    // Tối ưu hóa modal full screen cho mobile (bind once)
    function optimizeModalForMobile() {
        // Tránh bind nhiều lần khi gọi lại
        $('#productDetailModal').off('.mobilefs');

       
        
        $('#productDetailModal').on('hidden.bs.modal.mobilefs', function() {
            // Nothing here; global handler will restore scroll
        });
    }
    
    // Khởi tạo tối ưu hóa modal
    optimizeModalForMobile();
    
    // Không cần rebind theo resize; CSS đã xử lý responsive

    // ===== Scroll lock with position restore for ALL modals =====
    (function setupGlobalModalScrollLock(){
        var scrollPos = 0;
        var $body = $('body');
        var $html = $('html');
        
        // Kiểm tra nếu là mobile
        function isMobile() {
            return window.innerWidth <= 768;
        }
        
        $(document).on('show.bs.modal', '.modal', function(){
            // Chỉ lock khi chưa có modal nào mở
            if ($body.data('scroll-locked')) return;
            
            // Lưu vị trí scroll hiện tại
            scrollPos = window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop || 0;
            
            // Lock scroll với position fixed
            $body
                .data('scroll-locked', true)
                .data('scroll-position', scrollPos)
                .css({
                    position: 'fixed',
                    top: '-' + scrollPos + 'px',
                    left: '0',
                    right: '0',
                    width: '100%',
                    overflow: 'hidden'
                });
            
            // Thêm class để có thể style nếu cần
            $html.addClass('modal-scroll-locked');
        });

        $(document).on('hidden.bs.modal', '.modal', function(){
            // Đợi một chút để đảm bảo modal đã đóng hoàn toàn
            setTimeout(function(){
                // Nếu vẫn còn modal khác đang mở thì không restore
                if ($('.modal.show').length > 0) return;
                
                // Nếu là mobile thì reload trang
                if (isMobile()) {
                    location.reload();
                    return;
                }
                
                // Desktop: Restore scroll position bình thường
                var savedScrollPos = $body.data('scroll-position') || 0;
                
                // Remove lock từ từ để tránh reload
                $html.removeClass('modal-scroll-locked');
                
                $body
                    .removeData('scroll-locked')
                    .removeData('scroll-position');
                
                // Remove CSS styles và restore scroll cùng lúc trong một frame
                requestAnimationFrame(function(){
                    $body.css({
                        position: '',
                        top: '',
                        left: '',
                        right: '',
                        width: '',
                        overflow: ''
                    });
                    
                    // Chỉ restore scroll nếu có vị trí đã lưu
                    if (savedScrollPos > 0) {
                        // Sử dụng scrollTo với behavior smooth để tránh reload
                        try {
                            window.scrollTo({
                                top: savedScrollPos,
                                left: 0,
                                behavior: 'instant'
                            });
                        } catch (e) {
                            // Fallback cho trình duyệt cũ
                            document.documentElement.scrollTop = savedScrollPos;
                            document.body.scrollTop = savedScrollPos;
                        }
                    }
                });
            }, 50);
        });
    })();
    
    // Xử lý click vào dòng sản phẩm hoặc nút xem chi tiết
    $(document).on('click', '.clickable-row, .view-product', function (e) {
        // Nếu là nút view-product thì ngăn sự kiện nổi bọt lên tr
        if ($(e.target).closest('.view-product').length > 0) {
            e.stopPropagation();
        }
        var id = $(this).data('id') || $(this).closest('tr').data('id');
        if (!id) return;
        $('#productDetailModal').modal('show');
        $('#productDetailContent').html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div></div>');
        
        var detailUrl = window.productUrls.detail;
        $.get(detailUrl, { id: id }, function (data) {
            $('#productDetailContent').html(data);
        }).fail(function () {
            $('#productDetailContent').html('<div class="alert alert-danger">Không thể tải chi tiết sản phẩm.</div>');
        });
    });

    // ========== QUẢN LÝ THÀNH PHẦN ==========
    
    // Xử lý nút quản lý thành phần
    $('#manageThanhPhanBtn').on('click', function(e) {
        e.preventDefault();
        
        $('#thanhPhanModal').modal('show');
        loadThanhPhanList();
    });
    
    // Xử lý nút thêm thành phần mới
    $(document).on('click', '#addThanhPhanBtn', function(e) {
        e.preventDefault();
        showAddEditThanhPhanModal();
    });
    
    // Xử lý nút chỉnh sửa thành phần
    $(document).on('click', '.edit-thanhphan-btn', function(e) {
        e.preventDefault();
        
        var id = $(this).data('id');
        var ten = $(this).data('ten');
        
        showAddEditThanhPhanModal(id, ten);
    });
    
    // Xử lý nút xóa thành phần
    $(document).on('click', '.delete-thanhphan-btn', function(e) {
        e.preventDefault();
        
        var id = $(this).data('id');
        var ten = $(this).data('ten');
        
        showConfirmModal(
            'Xác nhận xóa thành phần',
            `Bạn có chắc chắn muốn xóa thành phần "<strong>${ten}</strong>"?<br><br><small class="text-muted">Thao tác này không thể hoàn tác.</small>`,
            function() {
                deleteThanhPhan(id);
            }
        );
    });
    
    // Xử lý nút lưu thành phần
    $(document).on('click', '#saveThanhPhanBtn', function(e) {
        e.preventDefault();
        saveThanhPhan();
    });
    
    // Functions cho quản lý thành phần
    function loadThanhPhanList() {
        $('#thanhPhanContent').html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="text-muted mt-2">Đang tải danh sách thành phần...</p></div>');
        
        $.get(window.productUrls.manageThanhPhan)
            .done(function(data) {
                $('#thanhPhanContent').html(data);
            })
            .fail(function() {
                $('#thanhPhanContent').html('<div class="alert alert-danger"><i class="fas fa-exclamation-triangle me-2"></i>Không thể tải danh sách thành phần.</div>');
            });
    }
    
    function showAddEditThanhPhanModal(id = 0, ten = '') {
        $('#idThanhPhan').val(id);
        $('#tenThanhPhan').val(ten);
        
        if (id > 0) {
            $('#modalTitle').text('Chỉnh sửa thành phần');
        } else {
            $('#modalTitle').text('Thêm thành phần mới');
        }
        
        $('#addEditThanhPhanModal').modal('show');
        setTimeout(() => $('#tenThanhPhan').focus(), 500);
    }
    
    function saveThanhPhan() {
        var $form = $('#addEditThanhPhanForm');
        var $saveBtn = $('#saveThanhPhanBtn');
        
        // Validate form
        var ten = $('#tenThanhPhan').val().trim();
        if (!ten) {
            showAlert('Vui lòng nhập tên thành phần.', 'warning');
            $('#tenThanhPhan').focus();
            return;
        }
        
        // Disable button and show loading
        var originalText = $saveBtn.html();
        $saveBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Đang lưu...');
        
        // Prepare form data
        var formData = $form.serialize();
        
        $.ajax({
            url: window.productUrls.addEditThanhPhan,
            type: 'POST',
            data: formData,
            success: function(response) {
                if (response.success) {
                    showAlert(response.message, 'success');
                    $('#addEditThanhPhanModal').modal('hide');
                    loadThanhPhanList(); // Reload list
                    
                    // Reload thành phần trong form sản phẩm nếu đang mở
                    if ($('#addEditProductForm').length > 0) {
                        loadThanhPhanForProduct();
                    }
                } else {
                    showAlert(response.message || 'Không thể lưu thành phần.', 'error');
                }
            },
            error: function() {
                showAlert('Lỗi kết nối. Vui lòng thử lại.', 'error');
            },
            complete: function() {
                $saveBtn.prop('disabled', false).html(originalText);
            }
        });
    }
    
    function deleteThanhPhan(id) {
        $.ajax({
            url: window.productUrls.deleteThanhPhan,
            type: 'POST',
            data: {
                id: id,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(response) {
                if (response.success) {
                    showAlert(response.message, 'success');
                    loadThanhPhanList(); // Reload list
                    
                    // Reload thành phần trong form sản phẩm nếu đang mở
                    if ($('#addEditProductForm').length > 0) {
                        loadThanhPhanForProduct();
                    }
                } else {
                    showAlert(response.message || 'Không thể xóa thành phần.', 'error');
                }
            },
            error: function() {
                showAlert('Lỗi kết nối. Vui lòng thử lại.', 'error');
            }
        });
    }

    // Function để load thành phần cho form sản phẩm (global function)
    window.loadThanhPhanForProduct = function() {
        var productId = $('input[name="IdSanPham"]').val() || 0;
        
        $('#thanhPhanContainer').html('<div class="text-center py-2"><small class="text-muted">Đang tải...</small></div>');
        
        $.get(window.productUrls.getThanhPhanForProduct, { productId: productId })
            .done(function(response) {
                if (response.success) {
                    var html = '';
                    if (response.data && response.data.length > 0) {
                        response.data.forEach(function(item) {
                            html += `
                                <div class="form-check mb-1">
                                    <input class="form-check-input thanhphan-checkbox" type="checkbox" 
                                           value="${item.id}" id="thanhphan_${item.id}" 
                                           ${item.isSelected ? 'checked' : ''}
                                           name="ThanhPhanIds">
                                    <label class="form-check-label" for="thanhphan_${item.id}">
                                        <strong>${item.ten}</strong>
                                    </label>
                                </div>
                            `;
                        });
                    } else {
                        html = '<div class="text-center py-2"><small class="text-muted">Chưa có thành phần nào. <a href="#" id="addFirstThanhPhan">Thêm thành phần đầu tiên</a></small></div>';
                    }
                    $('#thanhPhanContainer').html(html);
                    
                    // Xử lý click thêm thành phần đầu tiên
                    $('#addFirstThanhPhan').on('click', function(e) {
                        e.preventDefault();
                        $('#thanhPhanModal').modal('show');
                        loadThanhPhanList();
                    });
                } else {
                    $('#thanhPhanContainer').html('<div class="alert alert-warning">Không thể tải danh sách thành phần.</div>');
                }
            })
            .fail(function() {
                $('#thanhPhanContainer').html('<div class="alert alert-danger">Lỗi kết nối khi tải thành phần.</div>');
            });
    };

    // ========== QUẢN LÝ BIẾN THỂ TRONG MODAL CHI TIẾT ==========
    
    // Xử lý nút thêm biến thể trong modal chi tiết
    $(document).on('click', '#addVariantBtn', function(e) {
        e.preventDefault();
        
        var productId = $(this).data('product-id');
        if (!productId) return;
        
        loadVariantForm(null, productId);
        $('#variantModalLabel').text('Thêm biến thể');
        $('#variantModal').modal('show');
    });
    
    // Xử lý nút thông tin chi tiết sản phẩm
    $(document).on('click', '#productDetailsBtn', function(e) {
        e.preventDefault();
        
        var productId = $(this).data('product-id');
        if (!productId) return;
        
        $('#productDetailsModal').modal('show');
        $('#productDetailsContent').html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="text-muted mt-2">Đang tải dữ liệu...</p></div>');
        
        var detailUrl = window.productUrls.productDetails;
        $.get(detailUrl, { id: productId }, function (data) {
            $('#productDetailsContent').html(data);
        }).fail(function () {
            $('#productDetailsContent').html('<div class="alert alert-danger"><i class="fas fa-exclamation-triangle me-2"></i>Không thể tải thông tin chi tiết sản phẩm.</div>');
        });
    });

    // Xử lý nút chỉnh sửa biến thể
    $(document).on('click', '.edit-variant-btn', function(e) {
        e.preventDefault();
        
        var variantId = $(this).data('id');
        if (!variantId) return;
        
        loadVariantForm(variantId);
        $('#variantModalLabel').text('Chỉnh sửa biến thể');
        $('#variantModal').modal('show');
    });

    // Xử lý nút xóa biến thể
    $(document).on('click', '.delete-variant-btn', function(e) {
        e.preventDefault();
        
        var variantId = $(this).data('id');
        if (!variantId) return;
        
        showConfirmModal(
            'Xác nhận xóa biến thể',
            'Bạn có chắc chắn muốn xóa biến thể này?<br><br><small class="text-muted">Thao tác này không thể hoàn tác.</small>',
            function() {
                deleteVariant(variantId);
            }
        );
    });

    // Functions cho quản lý biến thể
    function loadVariantForm(variantId = null, productId = null) {
        $('#variantFormContent').html('<div class="text-center py-3"><div class="spinner-border text-primary" role="status"></div></div>');
        
        var url = window.productUrls.variantForm || '/admin/sanpham/variantform';
        var data = {};
        
        if (variantId) {
            data.variantId = variantId;
        }
        if (productId) {
            data.productId = productId;
        }
        
        $.get(url, data)
            .done(function(response) {
                $('#variantFormContent').html(response);
            })
            .fail(function() {
                $('#variantFormContent').html('<div class="alert alert-danger">Không thể tải form biến thể.</div>');
            });
    }

    function deleteVariant(variantId) {
        $.post(window.productUrls.deleteVariant || '/admin/sanpham/deletevariant', { 
            idBienThe: variantId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        })
            .done(function(response) {
                if (response.success) {
                    showToast(response.message, 'success');
                    // Reload chi tiết sản phẩm
                    setTimeout(() => {
                        var currentProductId = $('.edit-product-detail').data('id');
                        if (currentProductId) {
                            loadProductDetail(currentProductId);
                        }
                    }, 1000);
                } else {
                    showToast(response.message || 'Không thể xóa biến thể', 'error');
                }
            })
            .fail(function() {
                showToast('Lỗi kết nối', 'error');
            });
    }

    function loadAttributeManagement() {
        $('#attributeContent').html('<div class="text-center py-3"><div class="spinner-border text-primary" role="status"></div></div>');
        
        $.get(window.productUrls.attributeManagement || '/admin/sanpham/attributemanagement')
            .done(function(response) {
                $('#attributeContent').html(response);
            })
            .fail(function() {
                $('#attributeContent').html('<div class="alert alert-danger">Không thể tải quản lý thuộc tính.</div>');
            });
    }

    // Expose function globally để có thể gọi từ partial view
    window.loadAttributeManagement = loadAttributeManagement;

    function loadProductDetail(productId) {
        $('#productDetailContent').html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div></div>');
        
        $.get(window.productUrls.detail, { id: productId })
            .done(function(data) {
                $('#productDetailContent').html(data);
            })
            .fail(function() {
                $('#productDetailContent').html('<div class="alert alert-danger">Không thể tải chi tiết sản phẩm.</div>');
            });
    }

    // Xử lý submit form biến thể
    $(document).on('submit', '#variantForm', function(e) {
        e.preventDefault();
        
        var $form = $(this);
        var $btn = $form.find('button[type="submit"]');
        var originalText = $btn.text();
        
        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Đang lưu...');
        
        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize(),
            success: function(response) {
                if (response.success) {
                    showToast(response.message, 'success');
                    $('#variantModal').modal('hide');
                    
                    // Reload chi tiết sản phẩm
                    setTimeout(() => {
                        var currentProductId = $('.edit-product-detail').data('id');
                        if (currentProductId) {
                            loadProductDetail(currentProductId);
                        }
                    }, 1000);
                } else {
                    showToast(response.message || 'Có lỗi xảy ra', 'error');
                }
            },
            error: function() {
                showToast('Lỗi kết nối', 'error');
            },
            complete: function() {
                $btn.prop('disabled', false).text(originalText);
            }
        });
    });

    // Mở modal Thêm sản phẩm
    $('#addProductBtn').on('click', function (e) {
        e.preventDefault();
        $('#addEditProductModalLabel').text('Thêm sản phẩm mới');
        $('#addEditProductModal').modal('show');
        loadProductForm();
    });

    // Mở modal Quản lý thuộc tính
    $('#manageAttributesBtn').on('click', function (e) {
        e.preventDefault();
        $('#attributeModalLabel').text('Quản lý thuộc tính');
        $('#attributeModal').modal('show');
        loadAttributeManagement();
    });

    // Hàm tải form sản phẩm
    function loadProductForm(id = null) {
        $('#addEditProductContent').html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div></div>');
        var url = window.productUrls.addOrEdit;
        if (id) url += '?id=' + id;
        
        $.get(url, function (data) {
            $('#addEditProductContent').html(data);
        }).fail(function () {
            $('#addEditProductContent').html('<div class="alert alert-danger">Không thể tải form.</div>');
        });
    }

    // Submit form Thêm/Chỉnh sửa sản phẩm qua AJAX - chỉ gắn khi cần thiết
    $(document).off('submit', '#addEditProductForm').on('submit', '#addEditProductForm', function (e) {
        e.preventDefault();
        e.stopImmediatePropagation();
        
        var $form = $(this);
        var $submitBtn = $form.find('button[type="submit"]');
        
        // Kiểm tra nếu đang trong quá trình submit
        if ($submitBtn.prop('disabled') || $form.data('submitting')) {
            return false;
        }
        
        // Đánh dấu form đang submit
        $form.data('submitting', true);
        
        var formData = new FormData(this);
        
        // Thêm loading indicator
        var originalBtnText = $submitBtn.html();
        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Đang xử lý...');
        
        $.ajax({
            url: $form.attr('action'),
            type: $form.attr('method'),
            data: formData,
            processData: false,
            contentType: false,
            timeout: 30000,
            success: function (res) {
                if (res.success) {
                    // Thông báo thành công
                    showToast('Lưu sản phẩm thành công!', 'success');
                    $('#addEditProductModal').modal('hide');
                    setTimeout(() => location.reload(), 1000);
                } else {
                    // Hiển thị thông báo lỗi
                    if (res.message) {
                        showToast('Lỗi: ' + res.message, 'error');
                    } else if (res.html) {
                        // Cập nhật form với dữ liệu mới nếu có
                        $('#addEditProductContent').html(res.html);
                    } else {
                        showToast('Có lỗi xảy ra khi lưu sản phẩm!', 'error');
                    }
                }
            },
            error: function (xhr, textStatus, errorThrown) {
                // Xử lý lỗi AJAX
                var errorMessage = 'Có lỗi xảy ra khi gửi form!';
                if (textStatus === 'timeout') {
                    errorMessage = 'Yêu cầu quá thời gian chờ. Vui lòng thử lại.';
                } else if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
                
                showToast('Lỗi: ' + errorMessage, 'error');
                console.error('AJAX Error:', xhr);
            },
            complete: function() {
                // Reset trạng thái form
                $form.data('submitting', false);
                $submitBtn.prop('disabled', false).html(originalBtnText);
            }
        });
        
        return false;
    });
    
    // Xử lý nút chỉnh sửa trong modal chi tiết
    $(document).on('click', '.edit-product-detail', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        $('#productDetailModal').modal('hide');
        
        // Mở modal chỉnh sửa
        setTimeout(() => {
            $('#addEditProductModalLabel').text('Chỉnh sửa sản phẩm');
            $('#addEditProductModal').modal('show');
            loadProductForm(id);
        }, 300);
    });

    // Xử lý nút xóa trong modal chi tiết
    $(document).on('click', '.delete-product-detail', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        var $btn = $(this);
        
        // Lấy tên sản phẩm để hiển thị trong thông báo
        var productName = $('#productDetailModal .modal-body h4').first().text() || 'sản phẩm này';
        
        // Hiển thị modal xác nhận xóa
        showConfirmModal(
            'Xác nhận xóa sản phẩm',
            `Bạn có chắc chắn muốn XÓA VĨNH VIỄN sản phẩm "${productName}"?<br><br>
            <div class="alert alert-warning mb-0">
                <strong>⚠️ CẢNH BÁO:</strong><br>
                • Sản phẩm sẽ bị xóa hoàn toàn khỏi hệ thống<br>
                • Tất cả ảnh và thông tin liên quan sẽ bị xóa<br>
                • Thao tác này KHÔNG THỂ HOÀN TÁC!
            </div>`,
            function() {
                $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Đang xóa...');
                
                $.ajax({
                    url: window.productUrls.delete,
                    type: 'POST',
                    data: {
                        id: id,
                        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                    },
                    timeout: 30000,
                    success: function (response) {
                        if (response.success) {
                            showToast('Xóa sản phẩm thành công!', 'success');
                            $('#productDetailModal').modal('hide');
                            setTimeout(() => location.reload(), 1000);
                        } else {
                            showToast('Lỗi: ' + (response.message || 'Không thể xóa sản phẩm'), 'error');
                            $btn.prop('disabled', false).html('<i class="fas fa-trash me-1"></i> Xóa vĩnh viễn');
                        }
                    },
                    error: function (xhr, textStatus) {
                        var errorMsg = 'Lỗi kết nối máy chủ';
                        if (textStatus === 'timeout') {
                            errorMsg = 'Yêu cầu quá thời gian chờ. Vui lòng thử lại.';
                        } else if (xhr.responseJSON?.message) {
                            errorMsg = xhr.responseJSON.message;
                        }
                        showToast('Lỗi: ' + errorMsg, 'error');
                        $btn.prop('disabled', false).html('<i class="fas fa-trash me-1"></i> Xóa vĩnh viễn');
                    }
                });
            }
        );
    });
    
    $('#refreshPageBtn').on('click', function() {
        location.reload();
    });
});

// Custom notification functions
function showToast(message, type = 'info', duration = 5000) {
    const toastId = 'toast-' + Date.now();
    const iconMap = {
        success: 'fas fa-check-circle',
        error: 'fas fa-exclamation-circle',
        warning: 'fas fa-exclamation-triangle',
        info: 'fas fa-info-circle'
    };
    
    const bgMap = {
        success: 'bg-success',
        error: 'bg-danger',
        warning: 'bg-warning',
        info: 'bg-primary'
    };
    
    const toast = $(`
        <div id="${toastId}" class="toast align-items-center text-white ${bgMap[type]} border-0 mb-2" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="${iconMap[type]} me-2"></i>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `);
    
    // Create toast container if it doesn't exist
    if ($('#toast-container').length === 0) {
        $('body').append('<div id="toast-container" style="position: fixed; top: 20px; right: 20px; z-index: 9999;"></div>');
    }
    
    $('#toast-container').append(toast);
    
    // Initialize and show toast
    const bsToast = new bootstrap.Toast(toast[0], {
        autohide: true,
        delay: duration
    });
    bsToast.show();
    
    // Remove toast element after it's hidden
    toast.on('hidden.bs.toast', function() {
        $(this).remove();
    });
}

function showConfirmModal(title, message, onConfirm, onCancel = null) {
    const modalId = 'confirmModal-' + Date.now();
    const modal = $(`
        <div class="modal fade" id="${modalId}" tabindex="-1" data-bs-backdrop="static">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content border-0 shadow-lg">
                    <div class="modal-header bg-light border-0 pb-2">
                        <h5 class="modal-title fw-bold text-dark">
                            <i class="fas fa-exclamation-triangle text-warning me-2"></i>${title}
                        </h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Đóng"></button>
                    </div>
                    <div class="modal-body px-4 py-3">
                        ${message}
                    </div>
                    <div class="modal-footer bg-light border-0 pt-2">
                        <button type="button" class="btn btn-light border me-2" data-bs-dismiss="modal">
                            <i class="fas fa-times me-1"></i>Hủy bỏ
                        </button>
                        <button type="button" class="btn btn-danger confirm-btn">
                            <i class="fas fa-trash me-1"></i>Xác nhận xóa
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `);
    
    $('body').append(modal);
    
    // Handle confirm button click
    modal.find('.confirm-btn').on('click', function() {
        modal.modal('hide');
        if (onConfirm) onConfirm();
    });
    
    // Handle cancel
    modal.on('hidden.bs.modal', function() {
        $(this).remove();
        if (onCancel) onCancel();
    });
    
    // Show modal
    modal.modal('show');
}
