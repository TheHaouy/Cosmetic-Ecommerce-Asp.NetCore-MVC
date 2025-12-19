// Function để toggle timeline
function toggleTimeline(orderId, event) {
    event.stopPropagation(); // Ngăn không cho click vào row
    
    var wrapper = document.querySelector('.timeline-wrapper[data-order-id="' + orderId + '"]');
    var timelineList = wrapper.querySelector('.timeline-list');
    var icon = wrapper.querySelector('.timeline-toggle-icon');
    
    if (timelineList.style.display === 'none' || timelineList.style.display === '') {
        // Mở timeline
        timelineList.style.display = 'block';
        wrapper.classList.add('expanded');
    } else {
        // Đóng timeline
        timelineList.style.display = 'none';
        wrapper.classList.remove('expanded');
    }
}
