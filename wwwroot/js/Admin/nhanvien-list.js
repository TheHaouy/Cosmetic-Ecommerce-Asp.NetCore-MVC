$(document).ready(function () {
    // Lọc và tìm kiếm
    function filterTable() {
        var searchValue = $('#searchInput').val().toLowerCase();
        var statusValue = $('#statusFilter').val();
        var genderValue = $('#genderFilter').length ? $('#genderFilter').val() : '';

        var count = 0;

        $('#staffTable tbody tr').each(function () {
            var row = $(this);
            var text = row.text().toLowerCase();
            
            // Fix status detection based on NhanVien.cshtml classes
            var statusBadge = row.find('.badge.status');
            var status = statusBadge.hasClass('active') ? 'true' : 'false';
            
            // Fix gender detection (class .small instead of tag small)
            var gender = row.find('td:eq(2) .small').text();

            var matchSearch = text.includes(searchValue);
            var matchStatus = statusValue === '' || status === statusValue;
            var matchGender = genderValue === '' || gender.includes(genderValue);

            if (matchSearch && matchStatus && matchGender) {
                row.show();
                count++;
            } else {
                row.hide();
            }
        });

        $('#currentCount').text(count);
    }

    $('#searchInput, #statusFilter, #genderFilter').on('keyup change', function () {
        filterTable();
    });
});
