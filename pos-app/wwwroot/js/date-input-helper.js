// Date input helper functions for formatting dates to dd/mm/yyyy
window.dateInputHelper = {
    // Format date from yyyy-MM-dd (HTML5 date input format) to dd/mm/yyyy
    formatDateToDisplay: function (dateString) {
        if (!dateString) return '';
        
        // Parse yyyy-MM-dd format
        const date = new Date(dateString + 'T00:00:00');
        if (isNaN(date.getTime())) return '';
        
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        
        return `${day}/${month}/${year}`;
    },
    
    // Parse dd/mm/yyyy to yyyy-MM-dd (HTML5 date input format)
    parseDateFromDisplay: function (displayDate) {
        if (!displayDate) return '';
        
        // Check if it matches dd/mm/yyyy pattern
        const pattern = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/;
        const match = displayDate.match(pattern);
        
        if (!match) return '';
        
        const day = parseInt(match[1], 10);
        const month = parseInt(match[2], 10);
        const year = parseInt(match[3], 10);
        
        // Validate date
        const date = new Date(year, month - 1, day);
        if (date.getDate() !== day || date.getMonth() !== month - 1 || date.getFullYear() !== year) {
            return '';
        }
        
        // Format as yyyy-MM-dd
        return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    },
    
    // Sync calendar input to display input
    syncCalendarToDisplay: function (calendarInputId, displayInputId) {
        const calendarInput = document.getElementById(calendarInputId);
        const displayInput = document.getElementById(displayInputId);
        
        if (!calendarInput || !displayInput) return;
        
        if (calendarInput.value) {
            displayInput.value = window.dateInputHelper.formatDateToDisplay(calendarInput.value);
        } else {
            displayInput.value = '';
        }
    },
    
    // Sync display input to calendar input
    syncDisplayToCalendar: function (displayInputId, calendarInputId) {
        const displayInput = document.getElementById(displayInputId);
        const calendarInput = document.getElementById(calendarInputId);
        
        if (!displayInput || !calendarInput) return;
        
        const dateValue = window.dateInputHelper.parseDateFromDisplay(displayInput.value);
        if (dateValue) {
            calendarInput.value = dateValue;
        } else {
            calendarInput.value = '';
        }
    },
    
    // Open calendar picker
    openCalendar: function (calendarInputId) {
        const calendarInput = document.getElementById(calendarInputId);
        if (calendarInput) {
            calendarInput.showPicker();
        }
    }
};

