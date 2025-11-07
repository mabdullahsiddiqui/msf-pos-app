// File download functionality
window.downloadFile = function (filename, content, contentType) {
    const blob = new Blob([content], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};

// Excel export functionality
window.exportToExcel = function (data, filename) {
    // Create CSV content
    let csvContent = '';
    
    // Add headers
    const headers = Object.keys(data[0]);
    csvContent += headers.join(',') + '\n';
    
    // Add data rows
    data.forEach(row => {
        const values = headers.map(header => {
            const value = row[header];
            // Escape commas and quotes in values
            if (typeof value === 'string' && (value.includes(',') || value.includes('"'))) {
                return '"' + value.replace(/"/g, '""') + '"';
            }
            return value;
        });
        csvContent += values.join(',') + '\n';
    });
    
    // Download as CSV (Excel can open CSV files)
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', filename + '.csv');
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Cursor position tracking for input fields
window.getSelectionStart = function (element) {
    if (element && element.selectionStart !== undefined) {
        return element.selectionStart;
    }
    return 0;
};

window.getSelectionEnd = function (element) {
    if (element && element.selectionEnd !== undefined) {
        return element.selectionEnd;
    }
    return 0;
};

window.setSelectionRange = function (element, start, end) {
    if (element && element.setSelectionRange) {
        element.focus();
        element.setSelectionRange(start, end);
    }
};

window.getClipboardText = async function () {
    try {
        const text = await navigator.clipboard.readText();
        return text || "";
    } catch {
        return "";
    }
};

