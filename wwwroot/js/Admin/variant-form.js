$(document).ready(function () {
    // Ngăn chặn chọn nhiều giá trị cho cùng 1 thuộc tính
    $('.attribute-checkbox').on('change', function () {
        const attributeId = $(this).data('attribute');
        const isChecked = $(this).is(':checked');

        if (isChecked) {
            // Bỏ chọn các checkbox khác cùng thuộc tính
            $(`.attribute-checkbox[data-attribute="${attributeId}"]`).not(this).prop('checked', false);
        }
    });

    // Thêm thuộc tính mới
    $('#addAttributeBtn').on('click', function () {
        const newName = $('#newAttributeName').val().trim();
        if (!newName) {
            showToast('Vui lòng nhập tên thuộc tính.', 'warning');
            return;
        }

        const btn = $(this);
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm"></span>');

        $.post(window.variantUrls.addAttribute, { tenThuocTinh: newName, __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val() })
            .done(function (response) {
                if (response.success) {
                    showToast('Thêm thuộc tính thành công!', 'success');
                    $('#newAttributeName').val('');
                    // Tải lại trang để cập nhật danh sách thuộc tính
                    location.reload();
                } else {
                    showToast(response.message || 'Không thể thêm thuộc tính.', 'error');
                }
            })
            .fail(function () {
                showToast('Lỗi kết nối khi thêm thuộc tính.', 'error');
            })
            .always(function () {
                btn.prop('disabled', false).html('<i class="fas fa-plus me-1"></i> Thêm');
            });
    });

    // Thêm giá trị cho thuộc tính
    $('#addAttributeValuesBtn').on('click', function () {
        const attributeId = $('#selectAttributeForValue').val();
        const newValues = $('#newAttributeValues').val().trim();

        if (!attributeId) {
            showToast('Vui lòng chọn thuộc tính.', 'warning');
            return;
        }
        if (!newValues) {
            showToast('Vui lòng nhập giá trị.', 'warning');
            return;
        }

        const btn = $(this);
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm"></span>');

        $.post(window.variantUrls.addAttributeValue, {
            thuocTinhId: attributeId,
            giaTris: newValues,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        })
            .done(function (response) {
                if (response.success) {
                    showToast('Thêm giá trị thành công!', 'success');
                    $('#newAttributeValues').val('');
                    // Tải lại trang để cập nhật
                    location.reload();
                } else {
                    showToast(response.message || 'Không thể thêm giá trị.', 'error');
                }
            })
            .fail(function (xhr, status, error) {
                console.error('AddAttributeValue error:', xhr.status, xhr.responseText);
                showToast('Lỗi kết nối khi thêm giá trị: ' + (xhr.status || error), 'error');
            })
            .always(function () {
                btn.prop('disabled', false).html('<i class="fas fa-plus"></i> Thêm giá trị');
            });
    });


    // Form submission
    $('#variantForm').on('submit', function (e) {
        const form = $(this);
        if (!form[0].checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
            form.addClass('was-validated');
            return;
        }

        const $submitBtn = form.find('button[type="submit"]');
        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Đang lưu...');
    });

    function showToast(message, type = 'info') {
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: type,
            title: message,
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true
        });
    }
});
