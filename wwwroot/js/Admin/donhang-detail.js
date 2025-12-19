function updateStatus(orderId, newStatus) {
    if (!confirm(`Bạn có chắc muốn cập nhật trạng thái đơn hàng thành "${newStatus}"?`)) {
        return;
    }

    $.ajax({
        url: '/admin/donhang/updatestatus',
        type: 'POST',
        data: {
            id: orderId,
            status: newStatus
        },
        success: function (response) {
            if (response.success) {
                alert('Cập nhật trạng thái thành công!');
                location.reload();
            } else {
                alert('Có lỗi xảy ra!');
            }
        },
        error: function () {
            alert('Có lỗi xảy ra khi cập nhật trạng thái!');
        }
    });
}
