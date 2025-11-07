// Table filter functionality for column-based filtering
window.tableFilter = {
    // Initialize filters for a table
    initTableFilters: function (tableId, filterableColumns) {
        const container = document.getElementById(tableId);
        if (!container) return;

        // Find table element (could be the container itself or inside it)
        const table = container.tagName === 'TABLE' ? container : container.querySelector('table');
        if (!table) return;

        const thead = table.querySelector('thead');
        if (!thead) return;

        // Check if filter row already exists
        const existingFilterRow = thead.querySelector('.filter-row');
        if (existingFilterRow) return;

        // Get header row
        const headerRow = thead.querySelector('tr');
        if (!headerRow) return;

        // Create filter row
        const filterRow = document.createElement('tr');
        filterRow.className = 'filter-row';
        filterRow.style.backgroundColor = '#f8f9fa';

        const headerCells = headerRow.querySelectorAll('th');
        headerCells.forEach((headerCell, index) => {
            const filterCell = document.createElement('th');
            filterCell.style.padding = '4px';
            filterCell.style.border = '1px solid #dee2e6';

            // Only add filter if column is filterable
            if (filterableColumns.includes(index)) {
                const filterContainer = document.createElement('div');
                filterContainer.className = 'position-relative';
                filterContainer.style.padding = '0';

                const filterInput = document.createElement('input');
                filterInput.type = 'text';
                filterInput.className = 'form-control form-control-sm';
                filterInput.placeholder = `Filter ${headerCell.textContent.trim()}`;
                filterInput.style.padding = '2px 24px 2px 6px';
                filterInput.style.fontSize = '12px';
                filterInput.setAttribute('data-column-index', index);
                filterInput.setAttribute('data-table-id', tableId);
                
                // Store table reference for filtering
                if (!filterInput.dataset.tableRef) {
                    const tableElement = container.tagName === 'TABLE' ? container : container.querySelector('table');
                    if (tableElement) {
                        filterInput.dataset.tableRef = tableElement.id || tableId;
                    }
                }

                // Add clear button
                const clearButton = document.createElement('button');
                clearButton.type = 'button';
                clearButton.className = 'btn btn-sm position-absolute end-0';
                clearButton.style.cssText = 'background: none; border: none; color: #6c757d; padding: 0 4px; top: 50%; transform: translateY(-50%); z-index: 10; font-size: 14px;';
                clearButton.innerHTML = '×';
                clearButton.style.display = 'none';
                clearButton.setAttribute('data-column-index', index);
                clearButton.setAttribute('data-table-id', tableId);
                
                // Store table reference
                if (!clearButton.dataset.tableRef) {
                    const tableElement = container.tagName === 'TABLE' ? container : container.querySelector('table');
                    if (tableElement) {
                        clearButton.dataset.tableRef = tableElement.id || tableId;
                    }
                }

                // Event handlers
                filterInput.addEventListener('input', function () {
                    const value = this.value.trim();
                    clearButton.style.display = value ? 'block' : 'none';
                    window.tableFilter.applyFilters(tableId, filterableColumns);
                });

                clearButton.addEventListener('click', function () {
                    filterInput.value = '';
                    clearButton.style.display = 'none';
                    window.tableFilter.applyFilters(tableId, filterableColumns);
                    filterInput.focus();
                });

                filterContainer.appendChild(filterInput);
                filterContainer.appendChild(clearButton);
                filterCell.appendChild(filterContainer);
            }

            filterRow.appendChild(filterCell);
        });

        // Insert filter row after header row
        headerRow.parentNode.insertBefore(filterRow, headerRow.nextSibling);
        
        // Ensure table has an ID for filtering
        if (!table.id) {
            table.id = tableId + '-table';
        }
    },

    // Apply filters to table rows
    applyFilters: function (tableId, filterableColumns) {
        const container = document.getElementById(tableId);
        if (!container) return;

        // Find table element
        const table = container.tagName === 'TABLE' ? container : container.querySelector('table');
        if (!table) return;

        const tbody = table.querySelector('tbody');
        if (!tbody) return;

        // Get all filter values
        const filterValues = {};
        filterableColumns.forEach(index => {
            const filterInput = table.querySelector(`input[data-column-index="${index}"][data-table-id="${tableId}"]`);
            if (filterInput) {
                const value = filterInput.value.trim().toLowerCase();
                if (value) {
                    filterValues[index] = value;
                }
            }
        });

        // If no filters active, show all rows
        if (Object.keys(filterValues).length === 0) {
            tbody.querySelectorAll('tr').forEach(row => {
                row.style.display = '';
            });
            return;
        }

        // Filter rows
        const rows = tbody.querySelectorAll('tr');
        rows.forEach(row => {
            // Skip filter rows
            if (row.classList.contains('filter-row')) {
                return;
            }

            let shouldShow = true;

            // Check each active filter
            for (const [columnIndex, filterValue] of Object.entries(filterValues)) {
                const cellIndex = parseInt(columnIndex);
                const cells = row.querySelectorAll('td');
                
                if (cells.length > cellIndex) {
                    const cell = cells[cellIndex];
                    const cellText = cell.textContent.trim().toLowerCase();
                    
                    // Check if cell text contains filter value
                    if (!cellText.includes(filterValue)) {
                        shouldShow = false;
                        break;
                    }
                } else {
                    // Row doesn't have enough cells (might be a special row with colspan)
                    // Check if any cell text matches any of the filter values
                    let foundMatch = false;
                    for (const cell of cells) {
                        const cellText = cell.textContent.trim().toLowerCase();
                        if (cellText.includes(filterValue)) {
                            foundMatch = true;
                            break;
                        }
                    }
                    if (!foundMatch) {
                        shouldShow = false;
                        break;
                    }
                }
            }

            row.style.display = shouldShow ? '' : 'none';
        });
    },

    // Clear all filters for a table
    clearAllFilters: function (tableId, filterableColumns) {
        const container = document.getElementById(tableId);
        if (!container) return;

        const table = container.tagName === 'TABLE' ? container : container.querySelector('table');
        if (!table) return;

        filterableColumns.forEach(index => {
            const filterInput = table.querySelector(`input[data-column-index="${index}"][data-table-id="${tableId}"]`);
            const clearButton = table.querySelector(`button[data-column-index="${index}"][data-table-id="${tableId}"]`);
            
            if (filterInput) {
                filterInput.value = '';
            }
            if (clearButton) {
                clearButton.style.display = 'none';
            }
        });

        window.tableFilter.applyFilters(tableId, filterableColumns);
    }
};

