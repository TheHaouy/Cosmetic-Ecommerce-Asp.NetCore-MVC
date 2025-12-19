$(document).ready(function() {
    var currentCategoryId = null;
    var hasChanges = false; // Track if any changes were made in the modal
    var suppressReloadOnViewClose = false; // prevent reload when closing for delete flow

    // Truncate descriptions in table on page load
    truncateDescriptions();
    // Ensure description container in modal exists and is styled
    ensureDescContainer();

    // Load parent categories on modal open
    $('#addCategoryModal').on('show.bs.modal', function() {
        loadParentCategories('#categoryParent', null);
    });

    $('#editCategoryModal').on('show.bs.modal', function() {
        var currentId = $('#editCategoryId').val();
        loadParentCategories('#editCategoryParent', currentId);
    });

    // Reload page when view products modal is closed only if there were changes and not suppressed
    $('#viewProductsModal').on('hidden.bs.modal', function() {
        if (suppressReloadOnViewClose) {
            suppressReloadOnViewClose = false;
            return;
        }
        if (hasChanges) {
            location.reload();
        }
    });

    // Image upload handlers for Add Category
    $('#categoryImage').on('change', function() {
        handleImageUpload(this, '#imagePreview', '#previewImg', '#categoryImageUrl');
    });

    $('#removeImageBtn').on('click', function() {
        removeImage('#categoryImage', '#imagePreview', '#previewImg', '#categoryImageUrl');
    });

    // Image upload handlers for Edit Category
    $('#editCategoryImage').on('change', function() {
        handleImageUpload(this, '#editImagePreview', '#editPreviewImg', '#editCategoryImageUrl');
    });

    $('#editRemoveImageBtn').on('click', function() {
        removeImage('#editCategoryImage', '#editImagePreview', '#editPreviewImg', '#editCategoryImageUrl');
    });

    // Initialize sortable with drag and drop support for both parent and child categories
    var startX = 0;
    var startY = 0;
    
    $('#categoryTableBody').sortable({
        items: 'tr.parent-category, tr.child-category',  // Allow sorting both parent and child categories
        handle: '.sortable-handle',
        placeholder: 'sortable-placeholder',
        helper: function(e, tr) {
            var $originals = tr.children();
            var $helper = tr.clone();
            $helper.children().each(function(index) {
                $(this).width($originals.eq(index).width());
            });
            return $helper;
        },
        start: function(e, ui) {
            ui.placeholder.html('<td colspan="8"></td>');
            
            // Store starting position for indent detection
            startX = e.pageX;
            startY = e.pageY;
            ui.item.data('startX', startX);
            ui.item.data('startY', startY);
            ui.item.data('indent-intent', false);
            ui.item.data('outdent-intent', false);
            
            // Store original parent ID
            var originalParentId = ui.item.data('parent-id') || null;
            ui.item.data('original-parent-id', originalParentId);
            
            // Store children state if it's a parent category
            if (ui.item.hasClass('parent-category')) {
                var groupId = ui.item.data('group-id');
                var $children = $(`tr.child-category[data-group-id="${groupId}"]`);
                var childrenVisible = $children.length > 0 && $children.first().is(':visible');
                
                ui.item.data('children-visible', childrenVisible);
                
                // Collect all children rows
                var childrenHtml = [];
                $children.each(function() {
                    childrenHtml.push($(this)[0].outerHTML);
                });
                ui.item.data('children-html', childrenHtml);
                
                // Remove children during drag
                $children.remove();
            }
        },
        sort: function(e, ui) {
            // Track horizontal movement to detect indent/outdent intent
            var currentX = e.pageX;
            var startX = ui.item.data('startX');
            var deltaX = currentX - startX;
            
            var originalParentId = ui.item.data('original-parent-id');
            var isChild = ui.item.hasClass('child-category');
            
            // Increase threshold to 50px to prevent accidental indent/outdent
            var INDENT_THRESHOLD = 50;
            var OUTDENT_THRESHOLD = -50;
            
            // If dragged significantly to the right and not already a child, mark as indent intent
            if (deltaX > INDENT_THRESHOLD && !isChild) {
                // Check if there's a previous parent category to indent under
                var $prevParent = ui.placeholder.prevAll('tr.parent-category').first();
                if ($prevParent.length > 0) {
                    ui.item.data('indent-intent', true);
                    ui.item.data('outdent-intent', false);
                    ui.placeholder.addClass('indent-intent');
                    ui.placeholder.removeClass('outdent-intent');
                } else {
                    // No parent to indent under, reset
                    ui.item.data('indent-intent', false);
                    ui.item.data('outdent-intent', false);
                    ui.placeholder.removeClass('indent-intent outdent-intent');
                }
            } 
            // If dragged significantly to the left and is currently a child, mark as outdent intent
            else if (deltaX < OUTDENT_THRESHOLD && (isChild || originalParentId)) {
                ui.item.data('outdent-intent', true);
                ui.item.data('indent-intent', false);
                ui.placeholder.addClass('outdent-intent');
                ui.placeholder.removeClass('indent-intent');
            }
            else {
                // Reset if movement is within threshold
                ui.item.data('indent-intent', false);
                ui.item.data('outdent-intent', false);
                ui.placeholder.removeClass('indent-intent');
                ui.placeholder.removeClass('outdent-intent');
            }
        },
        over: function(e, ui) {
            // Add visual feedback when hovering over potential parent
            if (ui.item.data('indent-intent')) {
                ui.placeholder.prev('tr.parent-category').addClass('drop-target-hover');
            }
        },
        out: function(e, ui) {
            // Remove visual feedback
            $('tr.parent-category').removeClass('drop-target-hover');
        },
        beforeStop: function(e, ui) {
            // Re-insert children right after parent BEFORE sortable finalizes (only for parent categories)
            if (ui.item.hasClass('parent-category')) {
                var childrenHtml = ui.item.data('children-html');
                var childrenVisible = ui.item.data('children-visible');
                
                if (childrenHtml && childrenHtml.length > 0) {
                    // Insert each child after the parent
                    childrenHtml.forEach(function(html) {
                        ui.item.after(html);
                    });
                    
                    // Get the newly inserted children and set visibility
                    var groupId = ui.item.data('group-id');
                    var $newChildren = $(`tr.child-category[data-group-id="${groupId}"]`);
                    
                    if (childrenVisible) {
                        $newChildren.show();
                    } else {
                        $newChildren.hide();
                    }
                    
                    // Re-attach event handlers for new elements
                    reattachChildrenEvents($newChildren);
                }
                
                // NEW: keep expanded highlight in sync with visibility after re-insert
                if (childrenVisible) {
                    ui.item.addClass('expanded');
                } else {
                    ui.item.removeClass('expanded');
                }
            }
        },
        stop: function(e, ui) {
            // Remove all visual feedback
            $('tr.parent-category').removeClass('drop-target-hover');
            ui.placeholder.removeClass('indent-intent');
            ui.placeholder.removeClass('outdent-intent');
            
            var categoryId = ui.item.data('category-id');
            var indentIntent = ui.item.data('indent-intent');
            var outdentIntent = ui.item.data('outdent-intent');
            var originalParentId = ui.item.data('original-parent-id');
            var isChild = ui.item.hasClass('child-category');
            
            console.log('Stop event - categoryId:', categoryId, 'indent-intent:', indentIntent, 'outdent-intent:', outdentIntent, 'isChild:', isChild, 'originalParent:', originalParentId);
            
            // Determine new parent ID based on drag intent
            var newParentId = originalParentId;
            
            // Priority 1: Handle explicit outdent intent (kéo sang trái để tách ra)
            if (outdentIntent) {
                newParentId = null;
                console.log('Outdenting category', categoryId, 'to top-level');
            }
            // Priority 2: Handle explicit indent intent (kéo sang phải để gộp vào)
            else if (indentIntent && !isChild) {
                var $prevParent = ui.item.prevAll('tr.parent-category').first();
                if ($prevParent.length > 0) {
                    newParentId = $prevParent.data('category-id');
                    console.log('Indenting category', categoryId, 'under parent', newParentId);
                }
            }
            // Priority 3: If moving a child WITHOUT indent/outdent intent, keep its parent
            else if (isChild && !indentIntent && !outdentIntent) {
                // Keep the original parent - just reordering within the same group
                newParentId = originalParentId;
                console.log('Reordering child', categoryId, 'within same parent', originalParentId);
            }

            console.log('Final decision - Original parent:', originalParentId, 'New parent:', newParentId);
            
            // Check if parent actually changed
            var parentChanged = (originalParentId != newParentId);
            
            if (parentChanged) {
                // Parent changed - update via AJAX
                console.log('Parent changed, calling updateCategoryParent');
                updateCategoryParent(categoryId, newParentId);
            } else {
                // Just position changed - update order
                console.log('Only order changed, calling updateCategoryOrder');
                setTimeout(function() {
                    updateCategoryOrder();
                }, 100);
            }
        }
    });

    // Function to update category parent
    function updateCategoryParent(categoryId, newParentId) {
        $.ajax({
            url: window.danhmucUrls.updateParent,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                CategoryId: categoryId,
                NewParentId: newParentId
            }),
            success: function(response) {
                if (response.success) {
                    showToast('Cập nhật danh mục thành công!', 'success');
                    // Update the row's parent-id attribute
                    var $row = $(`tr[data-category-id="${categoryId}"]`);
                    if (newParentId) {
                        $row.attr('data-parent-id', newParentId);
                        $row.removeClass('parent-category').addClass('child-category');
                    } else {
                        $row.removeAttr('data-parent-id');
                        $row.removeClass('child-category').addClass('parent-category');
                    }
                    // Update display order numbers after parent change
                    updateDisplayOrderNumbers();
                } else {
                    showToast('Có lỗi: ' + response.message, 'error');
                    location.reload();
                }
            },
            error: function() {
                showToast('Có lỗi xảy ra khi cập nhật danh mục', 'error');
                location.reload();
            }
        });
    }

    // Edit button click: open edit modal
    $(document).on('click', '.edit-category-btn', function(e) {
        e.preventDefault();
        e.stopPropagation();
        var $row = $(this).closest('tr');
        openEditCategoryModalFromRow($row);
    });

    // Toggle children only when clicking the chevron icon
    $(document).on('click', '.toggle-icon', function(e) {
        e.stopPropagation();
        var $row = $(this).closest('tr.parent-category');
        var categoryId = $row.data('category-id');
        var hasChildren = $row.data('has-children');
        if (!hasChildren) return;

        var $children = $(`tr.child-category[data-parent-id="${categoryId}"]`);
        var isVisible = $children.first().is(':visible');
        if (isVisible) {
            $children.slideUp(200);
            $row.find('.toggle-icon').removeClass('rotate-down');
            $row.removeClass('expanded');
        } else {
            $children.slideDown(200);
            $row.find('.toggle-icon').addClass('rotate-down');
            $row.addClass('expanded');
        }
    });

    // helper to open edit modal from a table row (parent or child)
    window.openEditCategoryModalFromRow = function($row) {
        if (!$row || $row.length === 0) {
            console.warn('[DanhMuc] openEditCategoryModalFromRow: missing row');
            return;
        }
        const id = $row.data('category-id');
        const name = ($row.find('td:nth-child(4) .category-name').text() || '').trim();
        const $descSpan = $row.find('td:nth-child(5) span');
        const desc = ($descSpan.data('full-description') || $descSpan.text() || '').trim();
        const image = $row.data('image') || '';
        const parentId = $row.data('parent-id') || '';
        console.debug('[DanhMuc] Prefill edit modal', { id, name, parentId });

        // Set modal fields
        $('#editCategoryId').val(id);
        $('#editCategoryName').val(name);
        $('#editCategoryDescription').val(desc === 'Chưa có mô tả' ? '' : desc);

        // Prepare parent select: remember selected, options will be loaded on show event
        $('#editCategoryParent').data('selected-parent-id', parentId || '');

        // Set image preview
        $('#editCategoryImageUrl').val(image || '');
        if (image) {
            $('#editPreviewImg').attr('src', image);
            $('#editImagePreview').show();
        } else {
            $('#editPreviewImg').attr('src', '');
            $('#editImagePreview').hide();
        }

        // Show modal (triggers loadParentCategories via show.bs.modal handler)
        $('#editCategoryModal').modal('show');
    }

    // Search functionality - update to handle tree structure
    $('#searchInput').on('keyup', function() {
        var value = $(this).val().toLowerCase();
        
        if (value === '') {
            // Reset: show all parents, hide all children
            $('.parent-category').show().removeClass('expanded'); // NEW: reset highlight
            $('.child-category').hide();
            $('.toggle-icon').removeClass('rotate-down');
        } else {
            // Search in both parent and child categories
            $('.parent-category, .child-category').each(function() {
                var text = $(this).text().toLowerCase();
                var matches = text.indexOf(value) > -1;
                
                if ($(this).hasClass('parent-category')) {
                    $(this).toggle(matches);
                } else if ($(this).hasClass('child-category')) {
                    if (matches) {
                        $(this).show();
                        // Also show parent
                        var parentId = $(this).data('parent-id');
                        $(`.parent-category[data-category-id="${parentId}"]`).show();
                    } else {
                        $(this).hide();
                    }
                }
            });
        }
    });

    // View products button
    $(document).on('click', '.view-products', function(e) {
        e.stopPropagation();
        var categoryId = $(this).data('category-id');
        var categoryName = $(this).data('category-name');
        loadProductsForCategory(categoryId, categoryName);
    });

    // Add category form (no reload, update table inline)
    $('#addCategoryForm').submit(function(e) {
        e.preventDefault();
        
        var name = $('#categoryName').val().trim();
        var description = $('#categoryDescription').val().trim();
        var imageUrl = $('#categoryImageUrl').val();
        var parentId = $('#categoryParent').val();
        
        if (!name) {
            $('#categoryName').addClass('is-invalid');
            return;
        }
        if (!imageUrl) {
            $('#categoryImage').addClass('is-invalid');
            return;
        }
        
        $('#categoryName').removeClass('is-invalid');
        $('#categoryImage').removeClass('is-invalid');
        
        var data = {
            TenDanhMuc: name,
            MoTa: description || null,
            AnhDaiDien: imageUrl || null,
            IdDanhMucCha: parentId ? parseInt(parentId) : null
        };
        
        $.ajax({
            url: window.danhmucUrls.create,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function(response) {
                if (response.success && response.data) {
                    $('#addCategoryModal').modal('hide');
                    showToast('Thêm danh mục thành công!', 'success');

                    appendCategoryRow(response.data);
                    truncateDescriptions();
                    hasChanges = true;

                    if (!response.data.parentId) {
                        const optionHtml = `<option value="${response.data.id}">${response.data.name}</option>`;
                        $('#categoryParent').append(optionHtml);
                        $('#editCategoryParent').append(optionHtml);
                    }

                    $('#addCategoryForm')[0].reset();
                    removeImage('#categoryImage', '#imagePreview', '#previewImg', '#categoryImageUrl');
                } else {
                    showToast('Có lỗi xảy ra: ' + (response.message || 'Không nhận được dữ liệu trả về'), 'error');
                }
            },
            error: function() {
                showToast('Có lỗi xảy ra khi thêm danh mục', 'error');
            }
        });
    });

    // Edit category form
    $('#editCategoryForm').submit(function(e) {
        e.preventDefault();
        
        var id = $('#editCategoryId').val();
        var name = $('#editCategoryName').val().trim();
        var description = $('#editCategoryDescription').val().trim();
        var imageUrl = $('#editCategoryImageUrl').val();
        var parentId = $('#editCategoryParent').val();
        var $targetRow = $(`tr[data-category-id="${id}"]`);
        var oldParentId = $targetRow.data('parent-id') || null;
        
        if (!name) {
            $('#editCategoryName').addClass('is-invalid');
            return;
        }
        
        $('#editCategoryName').removeClass('is-invalid');
        
        var data = {
            Id: parseInt(id),
            TenDanhMuc: name,
            MoTa: description || null,
            AnhDaiDien: imageUrl || null,
            IdDanhMucCha: parentId ? parseInt(parentId) : null
        };
        
        $.ajax({
            url: window.danhmucUrls.update,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function(response) {
                if (response.success) {
                    $('#editCategoryModal').modal('hide');
                    showToast('Cập nhật danh mục thành công!', 'success');

                    // Cập nhật ngay trên bảng, không reload trang
                    updateCategoryRowAfterEdit({
                        id: parseInt(id),
                        name: name,
                        description: description,
                        imageUrl: imageUrl,
                        parentId: parentId ? parseInt(parentId) : null,
                        parentName: $('#editCategoryParent option:selected').text()
                    });

                    hasChanges = true;

                    // Nếu đổi danh mục cha, thông báo để người dùng có thể F5 hoặc kéo thả lại nếu cần
                    var oldParentNormalized = oldParentId ? parseInt(oldParentId) : null;
                    var newParentNormalized = parentId ? parseInt(parentId) : null;
                    if (oldParentNormalized !== newParentNormalized) {
                        showToast('Đã đổi danh mục cha. Nếu cần, hãy tải lại trang để cập nhật vị trí hiển thị.', 'info');
                    }
                } else {
                    showToast('Có lỗi xảy ra: ' + response.message, 'error');
                }
            },
            error: function() {
                showToast('Có lỗi xảy ra khi cập nhật danh mục', 'error');
            }
        });
    });

    // Reset forms when modals are hidden
    $('#addCategoryModal').on('hidden.bs.modal', function() {
        $('#addCategoryForm')[0].reset();
        $('#categoryName').removeClass('is-invalid');
        removeImage('#categoryImage', '#imagePreview', '#previewImg', '#categoryImageUrl');
    });

    $('#editCategoryModal').on('hidden.bs.modal', function() {
        $('#editCategoryForm')[0].reset();
        $('#editCategoryName').removeClass('is-invalid');
        removeImage('#editCategoryImage', '#editImagePreview', '#editPreviewImg', '#editCategoryImageUrl');
    });

    // Handle confirm delete button click
    $(document).on('click', '#confirmDeleteBtn', function() {
        var categoryId = $(this).data('category-id');
        $('#confirmDeleteModal').modal('hide');
        deleteCategory(categoryId);
    });

    // Inline edit category name events
    $(document).on('click', '#categoryNameDisplay', function() {
        enterEditMode('name');
    });

    $(document).on('click', '#categoryDescDisplay', function() {
        enterEditMode('description');
    });

    // Save buttons
    $(document).on('click', '#saveCategoryNameBtn', function() {
        saveCategoryName();
    });

    $(document).on('click', '#saveCategoryDescBtn', function() {
        saveCategoryDescription();
    });

    // Cancel buttons
    $(document).on('click', '#cancelCategoryNameBtn', function() {
        resetEditMode();
    });

    $(document).on('click', '#cancelCategoryDescBtn', function() {
        resetEditMode();
    });

    $(document).on('keypress', '#categoryNameEdit', function(e) {
        if (e.which == 13) { // Enter key
            saveCategoryName();
        }
    });

    $(document).on('keydown', '#categoryNameEdit', function(e) {
        if (e.which == 27) { // Escape key
            resetEditMode();
        }
    });

    $(document).on('keypress', '#categoryDescEdit', function(e) {
        if (e.which == 13) { // Enter key
            saveCategoryDescription();
        }
    });

    $(document).on('keydown', '#categoryDescEdit', function(e) {
        if (e.which == 27) { // Escape key
            resetEditMode();
        }
    });

    // Delete category from modal
    $(document).on('click', '#deleteCategoryBtn', function() {
        var categoryId = $('#currentCategoryId').val();
        var categoryName = $('#categoryNameDisplay').text();
        var productCount = parseInt($(this).attr('data-product-count')) || 0;

        // Suppress reload when closing View Products modal to show confirm dialog
        suppressReloadOnViewClose = true;

        // Close the products modal first
        $('#viewProductsModal').modal('hide');

        // Show confirmation modal
        showConfirmDeleteModal(categoryId, categoryName, productCount);
    });

    // Edit button click: open modal with prefilled data
    $(document).on('click', '.btn-edit-category', function(e) {
        e.stopPropagation();
        const $row = $(this).closest('tr');
        try {
            console.debug('[DanhMuc] Edit button clicked for row id=', $row.data('category-id'));
            openEditCategoryModalFromRow($row);
        } catch (err) {
            console.error('[DanhMuc] Failed to open edit modal:', err);
        }
    });
});

function deleteCategory(categoryId) {
    $.ajax({
        url: window.danhmucUrls.delete,
        type: 'POST',
        data: { id: categoryId },
        success: function(response) {
            if (response.success) {
                showToast('Xóa danh mục thành công!', 'success');
                // Close modal if it's open
                $('#viewProductsModal').modal('hide');
                setTimeout(() => location.reload(), 1000);
            } else {
                showToast('Có lỗi xảy ra: ' + response.message, 'error');
            }
        },
        error: function() {
            showToast('Có lỗi xảy ra khi xóa danh mục', 'error');
        }
    });
}

function updateCategoryOrder() {
    var categoryIds = [];
    
    // Get ALL categories (both parent and child) in their current order
    $('#categoryTableBody tr.parent-category, #categoryTableBody tr.child-category').each(function() {
        var categoryId = $(this).data('category-id');
        if (categoryId !== undefined && categoryId !== null) {
            categoryIds.push(categoryId);
        }
    });

    console.log('Updating order for categories:', categoryIds);

    if (categoryIds.length > 0) {
        $.ajax({
            url: window.danhmucUrls.updateOrder,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(categoryIds),
            success: function(response) {
                if (response.success) {
                    console.log('Order update response:', response);
                    showToast(response.message || 'Cập nhật thứ tự thành công!', 'success');
                    // Update display order numbers in the UI
                    updateDisplayOrderNumbers();
                } else {
                    showToast('Có lỗi xảy ra khi cập nhật thứ tự: ' + response.message, 'error');
                    location.reload();
                }
            },
            error: function(xhr, status, error) {
                console.error('Error updating order:', error);
                showToast('Có lỗi xảy ra khi cập nhật thứ tự', 'error');
                location.reload();
            }
        });
    } else {
        console.warn('No categories found to update order');
    }
}

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

function showConfirmDeleteModal(categoryId, categoryName, productCount) {
    $('#deleteCategoryName').text(categoryName);
    $('#confirmDeleteBtn').data('category-id', categoryId);
    
    if (productCount > 0) {
        $('#productCount').text(productCount);
        $('#deleteWarning').removeClass('d-none');
        $('#confirmDeleteBtn').prop('disabled', true).text('Không thể xóa');
    } else {
        $('#deleteWarning').addClass('d-none');
        $('#confirmDeleteBtn').prop('disabled', false).html('<i class="fas fa-trash me-1"></i>Xóa danh mục');
    }
    
    $('#confirmDeleteModal').modal('show');
}

function loadProductsForCategory(categoryId, categoryName) {
    // Reset change tracking when opening products modal
    hasChanges = false;

    // Set category name in modal
    $('#categoryNameDisplay').text(categoryName);
    $('#currentCategoryId').val(categoryId);
    
    // Load category description
    ensureDescContainer();
    loadCategoryDescription(categoryId);
    
    // Reset edit mode
    resetEditMode();
    
    // Show loading state
    $('#productsList').html(`
        <div class="text-center">
            <i class="fas fa-spinner fa-spin fa-2x text-muted"></i>
            <p class="mt-2">Đang tải danh sách sản phẩm...</p>
        </div>
    `);
    
    // Show modal
    $('#viewProductsModal').modal('show');
    
    // Load products via AJAX
    $.ajax({
        url: window.danhmucUrls.getCategoryProducts,
        type: 'GET',
        data: { id: categoryId },
        success: function(products) {
            // Store product count for delete functionality
            $('#deleteCategoryBtn').attr('data-product-count', products ? products.length : 0);
            
            if (products && products.length > 0) {
                let html = '<div class="row">';
                products.forEach(function(product) {
                    html += `
                        <div class="col-md-4 col-lg-3 mb-3">
                            <div class="card h-100 product-card">
                                <div class="position-relative">
                                    <img src="${product.mainImage || '/Images/noimage.jpg'}" 
                                         class="card-img-top product-image" 
                                         alt="${product.name || 'Sản phẩm'}"
                                         style="height: 140px; object-fit: cover;"
                                         onerror="this.src='/Images/noimage.jpg'">
                                    <span class="badge ${product.status === 'Hiển thị' ? 'bg-success' : 'bg-secondary'} position-absolute top-0 end-0 m-2">
                                        ${product.status || 'Không xác định'}
                                    </span>
                                </div>
                                <div class="card-body d-flex flex-column">
                                    <h6 class="card-title text-truncate" title="${product.name || 'Chưa có tên'}">
                                        ${product.name || 'Chưa có tên'}
                                    </h6>
                                    <div class="d-flex justify-content-between align-items-end mt-auto">
                                        <div class="product-price">
                                            <p class="card-text text-primary fw-bold mb-0">
                                                ${product.price || '0 ₫'}
                                            </p>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    `;
                });
                html += '</div>';
                $('#productsList').html(html);
            } else {
                $('#productsList').html(`
                    <div class="text-center py-4">
                        <i class="fas fa-box-open fa-3x text-muted mb-3"></i>
                        <h5 class="text-muted">Danh mục chưa có sản phẩm nào</h5>
                        <p class="text-muted">Thêm sản phẩm vào danh mục để hiển thị ở đây</p>
                    </div>
                `);
            }
        },
        error: function() {
            $('#productsList').html(`
                <div class="text-center py-4">
                    <i class="fas fa-exclamation-triangle fa-3x text-danger mb-3"></i>
                    <h5 class="text-danger">Có lỗi xảy ra</h5>
                    <p class="text-muted">Không thể tải danh sách sản phẩm</p>
                </div>
            `);
        }
    });
}

// Add new function to load category description
function loadCategoryDescription(categoryId) {
    // Find the category row to get full description from data attribute
    var $categoryRow = $(`tr[data-category-id="${categoryId}"]`);
    var $descriptionSpan = $categoryRow.find('td:nth-child(5) span');
    
    // Get full description from data attribute, fallback to text content
    var description = $descriptionSpan.data('full-description') || $descriptionSpan.text();
    
    if (description && description !== 'Chưa có mô tả') {
        $('#categoryDescDisplay').text(description);
    } else {
        $('#categoryDescDisplay').text('Chưa có mô tả');
    }
}

// Add function to truncate descriptions in table (hard cap 50 chars, 1-line)
function truncateDescriptions() {
    $('#categoryTableBody tr').each(function() {
        var $row = $(this);
        var $span = $row.find('td:nth-child(5) span.category-description, td:nth-child(5) span').first();
        if ($span.length === 0) return;

        var fullText = ($span.data('full-description') || $span.text() || '').trim();

        // Store full text for modal/tooltip
        $span.data('full-description', fullText);
        $span.attr('title', fullText);

        // Strict 50-char display with ellipsis
        var display = fullText.length > 50 ? fullText.substring(0, 50) + '...' : fullText;
        $span.text(display);

        // Ensure single-line ellipsis (hide overflow)
        $span.css({
            display: 'block',
            width: '100%',
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            textOverflow: 'ellipsis'
        });

        // Keep cell single-line to avoid pushing into action buttons
        $span.closest('td').css({ whiteSpace: 'nowrap' });
    });
}

function enterEditMode(type) {
    // Reset any existing edit mode first
    resetEditMode();
    
    if (type === 'name') {
        var currentName = $('#categoryNameDisplay').text();
        $('#categoryNameEdit').val(currentName);
        $('#categoryNameDisplay').addClass('d-none');
        $('#categoryNameEditGroup').removeClass('d-none');
        $('#categoryNameEdit').focus().select();
    } else if (type === 'description') {
        var currentDesc = $('#categoryDescDisplay').text();
        $('#categoryDescEdit').val(currentDesc === 'Chưa có mô tả' ? '' : currentDesc);
        $('#categoryDescDisplay').addClass('d-none');
        $('#categoryDescEditGroup').removeClass('d-none');
        $('#categoryDescEdit').focus().select();
        $('#categoryDescContainer').addClass('d-none');
    }
}

function resetEditMode() {
    $('#categoryNameDisplay').removeClass('d-none');
    $('#categoryDescDisplay').removeClass('d-none');
    $('#categoryNameEditGroup').addClass('d-none');
    $('#categoryDescEditGroup').addClass('d-none');
    $('#categoryDescContainer').removeClass('d-none');
}

function saveCategoryName() {
    var categoryId = $('#currentCategoryId').val();
    var newName = $('#categoryNameEdit').val().trim();
    var originalName = $('#categoryNameDisplay').text();
    
    if (!newName) {
        showToast('Tên danh mục không được để trống', 'error');
        $('#categoryNameEdit').focus();
        return;
    }
    
    // Check if anything changed
    if (newName === originalName) {
        resetEditMode();
        return;
    }
    
    $.ajax({
        url: window.danhmucUrls.update,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            Id: parseInt(categoryId),
            TenDanhMuc: newName,
            MoTa: null // Don't update description when editing name
        }),
        success: function(response) {
            if (response.success) {
                $('#categoryNameDisplay').text(newName);
                resetEditMode();
                showToast('Cập nhật tên danh mục thành công!', 'success');

                // Mark that there are changes so we can reload when modal closes
                hasChanges = true;

                // Update the name in the main table
                $(`tr[data-category-id="${categoryId}"]`).attr('data-category-name', newName);
                $(`tr[data-category-id="${categoryId}"] td:nth-child(4) strong`).text(newName);
            } else {
                showToast('Có lỗi xảy ra: ' + response.message, 'error');
            }
        },
        error: function() {
            showToast('Có lỗi xảy ra khi cập nhật tên danh mục', 'error');
        }
    });
}

function saveCategoryDescription() {
    var categoryId = $('#currentCategoryId').val();
    var newDesc = $('#categoryDescEdit').val().trim();
    var originalDesc = $('#categoryDescDisplay').text();
    var currentName = $('#categoryNameDisplay').text();
    
    // Check if anything changed
    if (newDesc === originalDesc || (newDesc === '' && originalDesc === 'Chưa có mô tả')) {
        resetEditMode();
        return;
    }
    
    $.ajax({
        url: window.danhmucUrls.update,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            Id: parseInt(categoryId),
            TenDanhMuc: currentName, // Keep current name
            MoTa: newDesc || null
        }),
        success: function(response) {
            if (response.success) {
                $('#categoryDescDisplay').text(newDesc || 'Chưa có mô tả');
                resetEditMode();
                showToast('Cập nhật mô tả danh mục thành công!', 'success');

                // Update the description in the main table and keep full text
                const $cellSpan = $(`tr[data-category-id="${categoryId}"] td:nth-child(5) span`);
                $cellSpan.text(newDesc || 'Chưa có mô tả')
                         .data('full-description', newDesc || 'Chưa có mô tả');

                // Re-apply single-line ellipsis styling
                truncateDescriptions();

                // Ensure container stays styled
                ensureDescContainer();

                // Mark that there are changes so we can reload when modal closes
                hasChanges = true;
            } else {
                showToast('Có lỗi xảy ra: ' + response.message, 'error');
            }
        },
        error: function() {
            showToast('Có lỗi xảy ra khi cập nhật mô tả danh mục', 'error');
        }
    });
}

// Cập nhật hàng danh mục trên bảng sau khi chỉnh sửa (không reload trang)
function updateCategoryRowAfterEdit(payload) {
    if (!payload || !payload.id) return;
    const $row = $(`tr[data-category-id="${payload.id}"]`);
    if ($row.length === 0) return;

    const newName = payload.name || '';
    const newDesc = payload.description || '';
    const newDescDisplay = newDesc || 'Chưa có mô tả';
    const newImage = payload.imageUrl || '';
    const newParentId = payload.parentId || null;
    const newParentName = (payload.parentName || '').trim();

    // Cập nhật tên
    $row.attr('data-category-name', newName);
    $row.find('td:nth-child(4) .category-name').text(newName);

    // Cập nhật mô tả + lưu full text
    const $descSpan = $row.find('td:nth-child(5) span');
    $descSpan.text(newDescDisplay)
             .data('full-description', newDescDisplay);
    truncateDescriptions();

    // Cập nhật ảnh
    const $imgCell = $row.find('td:nth-child(3)');
    if (newImage) {
        const imgHtml = `<img src="${newImage}" alt="${newName}" style="width: 50px; height: 50px; object-fit: cover; border-radius: 8px; border: 1px solid #ddd;">`;
        $imgCell.html(imgHtml);
        $row.attr('data-image', newImage);
    } else {
        $imgCell.html('<i class="fas fa-image fa-2x text-muted"></i>');
        $row.attr('data-image', '');
    }

    // Cập nhật thông tin cha (không di chuyển row để tránh thay đổi cấu trúc phức tạp)
    const $parentCell = $row.find('td:nth-child(6)');
    if (newParentId) {
        $row.attr('data-parent-id', newParentId);
        $parentCell.html(`<span class="badge bg-info">${newParentName || 'Danh mục cha'}</span>`);
    } else {
        $row.removeAttr('data-parent-id');
        $parentCell.html('<span class="text-muted">—</span>');
    }
}

// Thêm danh mục mới vào bảng mà không cần reload
function appendCategoryRow(data) {
    if (!data || !data.id) return;

    const id = data.id;
    const name = data.name || '';
    const description = data.description || '';
    const descDisplay = description || 'Chưa có mô tả';
    const imageUrl = data.image || '';
    const parentId = data.parentId || null;
    const parentName = data.parentName || 'Danh mục cha';
    const order = (data.order !== undefined && data.order !== null) ? data.order : 0;

    const imageHtml = imageUrl
        ? `<img src="${imageUrl}" alt="${name}" style="width: 50px; height: 50px; object-fit: cover; border-radius: 8px; border: 1px solid #ddd;">`
        : '<i class="fas fa-image fa-2x text-muted"></i>';

    if (parentId) {
        // Child category
        const groupId = `group-${parentId}`;
        const childHtml = `
            <tr class="child-category" 
                data-group-id="${groupId}"
                data-category-id="${id}" 
                data-category-name="${name}"
                data-parent-id="${parentId}"
                data-image="${imageUrl}"
                style="display: none;">
                <td class="text-center">
                    <i class="fas fa-grip-vertical sortable-handle text-muted" title="Kéo để sắp xếp"></i>
                </td>
                <td class="text-center"><span class="badge bg-light text-dark">${order}</span></td>
                <td class="text-center">${imageHtml}</td>
                <td class="text-start ps-5">
                    <i class="fas fa-level-up-alt fa-rotate-90 me-2 text-muted"></i>
                    <strong class="category-name">${name}</strong>
                </td>
                <td class="text-start"><span class="text-muted category-description">${descDisplay}</span></td>
                <td class="text-center">
                    <span class="badge bg-info">${parentName}</span>
                </td>
                <td class="text-center">
                    <button class="btn btn-sm btn-primary edit-category-btn" 
                            data-category-id="${id}" title="Chỉnh sửa">
                        <i class="fas fa-edit"></i>
                    </button>
                </td>
            </tr>`;

        const $parentRow = $(`tr.parent-category[data-category-id="${parentId}"]`);
        if ($parentRow.length) {
            const $lastChild = $(`tr.child-category[data-parent-id="${parentId}"]`).last();
            if ($lastChild.length) {
                $lastChild.after(childHtml);
            } else {
                $parentRow.after(childHtml);
            }

            const $newChild = $(`tr.child-category[data-category-id="${id}"]`);
            const shouldShow = $parentRow.hasClass('expanded');
            if (shouldShow) {
                $newChild.show();
            }

            // Update parent to reflect it has children
            $parentRow.attr('data-has-children', 'true');
            ensureParentToggleAndBadge($parentRow);
        }
    } else {
        // Parent category
        const parentHtml = `
            <tr class="parent-category category-group" 
                data-group-id="group-${id}"
                data-category-id="${id}" 
                data-category-name="${name}"
                data-image="${imageUrl}"
                data-has-children="false"
                style="background-color: #f8f9fa; font-weight: 500;">
                <td class="text-center"><i class="fas fa-grip-vertical sortable-handle text-muted"></i></td>
                <td class="text-center">${order}</td>
                <td class="text-center">${imageHtml}</td>
                <td class="text-start">
                    <strong class="category-name">${name}</strong>
                </td>
                <td class="text-start"><span class="text-muted category-description">${descDisplay}</span></td>
                <td class="text-center">
                    <span class="text-muted">—</span>
                </td>
                <td class="text-center">
                    <button class="btn btn-sm btn-primary edit-category-btn" 
                            data-category-id="${id}" title="Chỉnh sửa">
                        <i class="fas fa-edit"></i>
                    </button>
                </td>
            </tr>`;

        $('#categoryTableBody').append(parentHtml);
    }
}

// Đảm bảo parent hiển thị toggle icon và badge số con khi có con mới
function ensureParentToggleAndBadge($parentRow) {
    if (!$parentRow || !$parentRow.length) return;
    const parentId = $parentRow.data('category-id');
    const childCount = $(`tr.child-category[data-parent-id="${parentId}"]`).length;
    const $nameCell = $parentRow.find('td:nth-child(4)');

    // Toggle icon
    if ($nameCell.find('.toggle-icon').length === 0) {
        $nameCell.prepend('<i class="fas fa-chevron-right toggle-icon me-2" style="transition: transform 0.3s; cursor: pointer;"></i>');
    }

    // Badge count
    const badgeText = `${childCount} con`;
    const $badge = $nameCell.find('.child-count-badge');
    if ($badge.length) {
        $badge.text(badgeText);
    } else {
        $nameCell.append(`<span class="badge bg-secondary ms-2 child-count-badge">${badgeText}</span>`);
    }
}

// Ensure a container wraps the modal description and constrain its display
function ensureDescContainer() {
    var $display = $('#categoryDescDisplay');
    if ($display.length === 0) return;

    // Wrap once
    if (!$display.parent().hasClass('desc-container')) {
        $display.wrap('<div id="categoryDescContainer" class="desc-container"></div>');
    }

    // Prefer the marked group; fallback to previous selector if needed
    var $leftGroup = $display.closest('.desc-area');
    if ($leftGroup.length === 0) {
        $leftGroup = $display.closest('.d-flex.align-items-center');
    }

    // Let the left group take the remaining space naturally
    $leftGroup.css({
        flex: '1 1 auto',
        maxWidth: 'none',
        minWidth: 0
    });

    // Insert container right after the label "Mô tả:" to keep order logical
    var $label = $leftGroup.find('small.text-muted').first();
    if ($label.length) {
        $label.after($('#categoryDescContainer'));
    }

    // Apply container styles
    $('#categoryDescContainer').css({
        maxHeight: '180px',
        overflowY: 'auto',
        overflowX: 'hidden',
        padding: '.375rem .75rem',
        backgroundColor: '#fff',
        border: '1px solid #198754',
        borderRadius: '.375rem',
        boxShadow: '0 0 0 .25rem rgba(25,135,84,.25)',
        wordBreak: 'break-word',
        whiteSpace: 'pre-wrap',
        lineHeight: '1.5',
        fontSize: '1rem',
        width: '100%',
        marginLeft: '0'
    });
}

// Load parent categories function (only show top-level categories as parents)
// Enhance to auto-select previously stored parent id if present on the select
function loadParentCategories(selectElement, excludeId) {
    // Collect allowed top-level ids from the table (parent-category rows)
    var allowedTopLevelIds = $('#categoryTableBody tr.parent-category')
        .map(function() { return $(this).data('category-id'); })
        .get();
    var allowedSet = new Set(allowedTopLevelIds);

    $.ajax({
        url: window.danhmucUrls.getParentCategories,
        type: 'GET',
        success: function(categories) {
            var $select = $(selectElement);
            $select.find('option:not(:first)').remove();

            var appended = 0;
            if (Array.isArray(categories) && categories.length > 0) {
                categories.forEach(function(category) {
                    if (!allowedSet.has(category.id)) return;
                    if (excludeId && category.id == excludeId) return;
                    $select.append(`<option value="${category.id}">${category.name}</option>`);
                    appended++;
                });
            }

            if (appended === 0) {
                $('#categoryTableBody tr.parent-category').each(function() {
                    var id = $(this).data('category-id');
                    if (excludeId && id == excludeId) return;
                    var name = $(this).find('td:nth-child(4) .category-name').text().trim()
                               || $(this).data('category-name');
                    if (id && name) {
                        $select.append(`<option value="${id}">${name}</option>`);
                    }
                });
            }

            // Auto-select stored parent id if any
            var sel = $select.data('selected-parent-id');
            if (sel !== undefined && sel !== null && sel !== '') {
                $select.val(String(sel));
            } else {
                $select.val('');
            }
        },
        error: function(xhr, status, error) {
            console.error('Không thể tải danh sách danh mục cha', error);
            showToast('Không thể tải danh sách danh mục', 'error');

            var $select = $(selectElement);
            $select.find('option:not(:first)').remove();
            $('#categoryTableBody tr.parent-category').each(function() {
                var id = $(this).data('category-id');
                if (excludeId && id == excludeId) return;
                var name = $(this).find('td:nth-child(4) .category-name').text().trim()
                           || $(this).data('category-name');
                if (id && name) {
                    $select.append(`<option value="${id}">${name}</option>`);
                }
            });

            var sel = $select.data('selected-parent-id');
            if (sel !== undefined && sel !== null && sel !== '') {
                $select.val(String(sel));
            } else {
                $select.val('');
            }
        }
    });
}

// Handle image upload
function handleImageUpload(input, previewContainer, previewImg, hiddenInput) {
    var file = input.files[0];
    
    if (!file) return;
    
    // Validate file type
    var allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
        showToast('Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp)', 'error');
        input.value = '';
        return;
    }
    
    // Validate file size (5MB)
    if (file.size > 5 * 1024 * 1024) {
        showToast('Kích thước file không được vượt quá 5MB', 'error');
        input.value = '';
        return;
    }
    
    // Show preview
    var reader = new FileReader();
    reader.onload = function(e) {
        $(previewImg).attr('src', e.target.result);
        $(previewContainer).show();
    };
    reader.readAsDataURL(file);
    
    // Upload to server
    var formData = new FormData();
    formData.append('file', file);
    
    $.ajax({
        url: window.danhmucUrls.uploadImage,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function(response) {
            if (response.success) {
                $(hiddenInput).val(response.imageUrl);
                showToast('Upload ảnh thành công!', 'success');
            } else {
                showToast('Có lỗi: ' + response.message, 'error');
                removeImage(input, previewContainer, previewImg, hiddenInput);
            }
        },
        error: function() {
            showToast('Có lỗi xảy ra khi upload ảnh', 'error');
            removeImage(input, previewContainer, previewImg, hiddenInput);
        }
    });
}

// Remove image
function removeImage(inputElement, previewContainer, previewImg, hiddenInput) {
    $(inputElement).val('');
    $(previewImg).attr('src', '');
    $(previewContainer).hide();
    $(hiddenInput).val('');
}

// Reattach event handlers to children after re-inserting
function reattachChildrenEvents($children) {
    // Events are delegated using $(document).on(), so they should work automatically
    // This function is here for future enhancements if needed
}

// Update display order numbers in the UI after reordering
function updateDisplayOrderNumbers() {
    var parentOrder = {};
    var parentCount = 0;
    
    // Loop through all visible rows to update order numbers
    $('#categoryTableBody tr.parent-category, #categoryTableBody tr.child-category').each(function() {
        var $row = $(this);
        var parentId = $row.data('parent-id');
        var $sttCell = $row.find('td:nth-child(2)');
        
        if ($row.hasClass('parent-category')) {
            // Parent category - sequential order starting from 1
            parentCount++;
            $sttCell.text(parentCount);
        } else if ($row.hasClass('child-category')) {
            // Child category - order within parent group starting from 1
            if (!parentOrder[parentId]) {
                parentOrder[parentId] = 0;
            }
            parentOrder[parentId]++;
            $sttCell.html(`<span class="badge bg-light text-dark">${parentOrder[parentId]}</span>`);
        }
    });
    
    console.log('Display order numbers updated');
}

