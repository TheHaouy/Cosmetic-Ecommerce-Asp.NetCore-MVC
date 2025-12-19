// ===========================
// TINHTHANHPHO.COM API - DỮ LIỆU SAU SÁP NHẬP
// ===========================
const API_BASE = 'https://www.tinhthanhpho.com/api/v1';

// Cache data để giảm số lần gọi API
let provincesData = [];
let communesCache = {}; // Cache cho xã/phường

/**
 * Load danh sách tỉnh/thành phố
 */
async function loadProvinces() {
    if (provincesData.length > 0) {
        return provincesData; // Trả về cache nếu đã load
    }

    try {
        const url = `${API_BASE}/new-provinces?limit=100`;
        console.log('🔄 Loading provinces from:', url);
        const response = await fetch(url);
        const result = await response.json();
        
        if (!result.success) {
            console.error('API error:', result.message);
            return [];
        }
        
        const data = result.data || [];
        console.log('✅ Provinces loaded:', data.length, 'provinces (sau sáp nhập 01/07/2025)');
        if (data.length > 0) {
            console.log('📍 Sample province:', data[0]);
        }
        
        // API trả về: {success: true, data: [{code, name, type}], metadata: {total, page, limit}}
        provincesData = data.map(p => ({
            id: p.code,
            code: p.code,
            name: p.name,
            shortName: p.name,
            type: p.type
        }));
        return provincesData;
    } catch (error) {
        console.error('Error loading provinces:', error);
        return [];
    }
}

/**
 * Load danh sách phường/xã theo tỉnh/thành phố
 */
async function loadCommunesByProvince(provinceCode) {
    // Kiểm tra cache
    if (communesCache[provinceCode]) {
        console.log('Using cached communes for province:', provinceCode);
        return communesCache[provinceCode];
    }

    try {
        const url = `${API_BASE}/new-provinces/${provinceCode}/wards?limit=1000`;
        console.log('Loading wards from:', url);
        const response = await fetch(url);
        const result = await response.json();
        
        if (!result.success) {
            console.error('API error:', result.message);
            return [];
        }
        
        const wards = result.data || [];
        
        // API trả về: {success: true, data: [{code, name, type, province_code}], metadata: {total, page, limit}}
        const allCommunes = wards.map(w => ({
            id: w.code,
            code: w.code,
            name: w.name,
            shortName: w.name,
            type: w.type,
            provinceCode: w.province_code || provinceCode
        }));
        
        console.log('Wards loaded:', allCommunes.length, 'wards for province', provinceCode, '(sau sáp nhập 01/07/2025)');
        if (allCommunes.length > 0) {
            console.log('Sample ward:', allCommunes[0]);
        }
        
        // Cache the result
        communesCache[provinceCode] = allCommunes;
        return allCommunes;
    } catch (error) {
        console.error('Error loading wards:', error);
        return [];
    }
}

/**
 * Tìm kiếm tỉnh/thành phố theo tên
 */
function searchProvinces(keyword) {
    if (!keyword || keyword.trim() === '') {
        return provincesData;
    }

    const lowerKeyword = keyword.toLowerCase().trim();
    return provincesData.filter(province => 
        province.name.toLowerCase().includes(lowerKeyword)
    );
}

/**
 * Tìm kiếm phường/xã theo tên
 */
function searchCommunes(communes, keyword) {
    if (!keyword || keyword.trim() === '') {
        return communes;
    }

    const lowerKeyword = keyword.toLowerCase().trim();
    return communes.filter(commune => 
        commune.name.toLowerCase().includes(lowerKeyword)
    );
}

/**
 * Khởi tạo autocomplete cho input tỉnh/thành phố
 */
function initProvinceAutocomplete(inputElement, dropdownElement, onSelect) {
    if (!inputElement || !dropdownElement) {
        console.error('[Address API] Province input or dropdown not found');
        return;
    }

    let selectedProvinceId = null;

    // Load provinces khi focus
    inputElement.addEventListener('focus', async () => {
        await loadProvinces();
        showProvinceDropdown(inputElement, dropdownElement, provincesData, onSelect);
    });

    // Tìm kiếm khi gõ
    inputElement.addEventListener('input', async () => {
        await loadProvinces(); // Đảm bảo data đã load
        const keyword = inputElement.value;
        const filtered = searchProvinces(keyword);
        showProvinceDropdown(inputElement, dropdownElement, filtered, onSelect);
        // Reset selected flag when user types
        if (inputElement.dataset.selected === 'true') {
            delete inputElement.dataset.selected;
        }
    });

    // Ẩn dropdown khi blur
    inputElement.addEventListener('blur', () => {
        setTimeout(() => {
            dropdownElement.classList.remove('show');
        }, 200);
    });

    return {
        getSelectedProvinceId: () => selectedProvinceId,
        setSelectedProvince: (id) => { selectedProvinceId = id; }
    };
}

/**
 * Hiển thị dropdown tỉnh/thành phố
 */
function showProvinceDropdown(inputElement, dropdownElement, provinces, onSelect) {
    dropdownElement.innerHTML = '';
    
    if (provinces.length === 0) {
        const noResult = document.createElement('div');
        noResult.className = 'dropdown-item';
        noResult.textContent = 'Không tìm thấy';
        noResult.style.cssText = 'padding: 12px; color: #999; text-align: center; font-size: 14px;';
        dropdownElement.appendChild(noResult);
        dropdownElement.classList.add('show');
        return;
    }

    provinces.forEach(province => {
        const item = document.createElement('div');
        item.className = 'dropdown-item';
        item.innerHTML = `${province.name}`;
        
        item.addEventListener('click', () => {
            inputElement.value = province.name;
            inputElement.dataset.provinceId = province.id;
            inputElement.dataset.selected = 'true';
            dropdownElement.classList.remove('show');
            
            if (onSelect) {
                onSelect(province);
            }
        });
        
        dropdownElement.appendChild(item);
    });
    
    dropdownElement.classList.add('show');
}

/**
 * Khởi tạo autocomplete cho input phường/xã
 */
function initCommuneAutocomplete(inputElement, dropdownElement, provinceInputElement, onSelect) {
    if (!inputElement || !dropdownElement || !provinceInputElement) {
        console.error('[Address API] Commune input, dropdown or province input not found');
        return;
    }

    let currentCommunes = [];
    let selectedCommuneId = null;

    // Load communes khi focus
    inputElement.addEventListener('focus', async () => {
        const provinceId = provinceInputElement.dataset.provinceId;
        
        if (!provinceId) {
            const noProvince = document.createElement('div');
            noProvince.className = 'dropdown-item';
            noProvince.textContent = 'Vui lòng chọn tỉnh/thành phố trước';
            noProvince.style.cssText = 'padding: 10px; color: #999; text-align: center;';
            dropdownElement.innerHTML = '';
            dropdownElement.appendChild(noProvince);
            dropdownElement.classList.add('show');
            return;
        }

        currentCommunes = await loadCommunesByProvince(provinceId);
        showCommuneDropdown(inputElement, dropdownElement, currentCommunes, onSelect);
    });

    // Tìm kiếm khi gõ
    inputElement.addEventListener('input', () => {
        const keyword = inputElement.value;
        const filtered = searchCommunes(currentCommunes, keyword);
        showCommuneDropdown(inputElement, dropdownElement, filtered, onSelect);
        // Reset selected flag when user types
        if (inputElement.dataset.selected === 'true') {
            delete inputElement.dataset.selected;
        }
    });

    // Ẩn dropdown khi blur
    inputElement.addEventListener('blur', () => {
        setTimeout(() => {
            dropdownElement.classList.remove('show');
        }, 200);
    });

    return {
        getSelectedCommuneId: () => selectedCommuneId,
        setSelectedCommune: (id) => { selectedCommuneId = id; }
    };
}

/**
 * Hiển thị dropdown phường/xã
 */
function showCommuneDropdown(inputElement, dropdownElement, communes, onSelect) {
    dropdownElement.innerHTML = '';
    
    if (communes.length === 0) {
        const noResult = document.createElement('div');
        noResult.className = 'dropdown-item';
        noResult.textContent = 'Không tìm thấy';
        noResult.style.cssText = 'padding: 12px; color: #999; text-align: center; font-size: 14px;';
        dropdownElement.appendChild(noResult);
        dropdownElement.classList.add('show');
        return;
    }

    communes.forEach(commune => {
        const item = document.createElement('div');
        item.className = 'dropdown-item';
        item.innerHTML = `${commune.name}`;
        
        item.addEventListener('click', () => {
            inputElement.value = commune.name;
            inputElement.dataset.communeId = commune.id;
            inputElement.dataset.selected = 'true';
            dropdownElement.classList.remove('show');
            
            if (onSelect) {
                onSelect(commune);
            }
        });
        
        dropdownElement.appendChild(item);
    });

    dropdownElement.classList.add('show');
}

/**
 * Setup address autocomplete - wrapper function for backward compatibility
 * @param {string} inputId - ID của input element
 * @param {string} dropdownId - ID của dropdown element
 * @param {string} type - Loại autocomplete: 'province' hoặc 'commune'
 */
function setupAddressAutocomplete(inputId, dropdownId, type) {
    console.log(`[Address API] Setting up ${type} autocomplete for ${inputId}`);
    
    const inputElement = document.getElementById(inputId);
    const dropdownElement = document.getElementById(dropdownId);
    
    if (!inputElement || !dropdownElement) {
        console.error(`[Address API] Missing elements for ${type}:`, { inputElement, dropdownElement });
        return;
    }
    
    if (type === 'province') {
        // Setup province autocomplete
        initProvinceAutocomplete(inputElement, dropdownElement, (province) => {
            console.log('[Address API] Province selected:', province);
            // Trigger commune autocomplete to load data for this province
            const communeInput = document.getElementById('inputWard');
            if (communeInput) {
                communeInput.value = '';
                communeInput.disabled = false;
                communeInput.placeholder = 'Nhập phường/xã...';
            }
        });
    } else if (type === 'commune') {
        // Setup commune autocomplete
        const provinceInput = document.getElementById('inputCity');
        if (!provinceInput) {
            console.error('[Address API] Province input not found for commune autocomplete');
            return;
        }
        
        initCommuneAutocomplete(inputElement, dropdownElement, provinceInput, (commune) => {
            console.log('[Address API] Commune selected:', commune);
        });
    }
}
