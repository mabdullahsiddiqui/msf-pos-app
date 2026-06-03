window.chartHelper = {
    charts: {},
    
    initialize: function() {
        if (typeof ChartDataLabels !== 'undefined') {
            Chart.register(ChartDataLabels);
        }
    },
    
    renderDashboardChart: function (canvasId, data) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        
        // If chart already exists, destroy it
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }
        
        const labels = data.map(d => d.transMonth);
        const salesData = data.map(d => d.sales);
        const recoveryData = data.map(d => d.recAmt);
        const expenseData = data.map(d => d.expAmt);
        
        this.charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Sales',
                        data: salesData,
                        backgroundColor: 'rgba(54, 162, 235, 0.7)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 1,
                        borderRadius: 5,
                        order: 2
                    },
                    {
                        label: 'Recovery',
                        data: recoveryData,
                        backgroundColor: 'rgba(0, 200, 83, 0.7)',
                        borderColor: 'rgba(0, 200, 83, 1)',
                        borderWidth: 1,
                        borderRadius: 5,
                        order: 2
                    },
                    {
                        label: 'Expenses',
                        data: expenseData,
                        type: 'line',
                        borderColor: 'rgba(255, 23, 68, 1)',
                        backgroundColor: 'rgba(255, 23, 68, 0.1)',
                        borderWidth: 3,
                        pointBackgroundColor: 'rgba(255, 23, 68, 1)',
                        pointRadius: 4,
                        fill: false,
                        tension: 0.4,
                        order: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            padding: 20,
                            font: {
                                size: 12
                            }
                        }
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        backgroundColor: 'rgba(255, 255, 255, 0.9)',
                        titleColor: '#333',
                        bodyColor: '#666',
                        borderColor: '#ddd',
                        borderWidth: 1,
                        padding: 10,
                        callbacks: {
                            label: function(context) {
                                let label = context.dataset.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                if (context.parsed.y !== null) {
                                    label += new Intl.NumberFormat('en-US', { style: 'currency', currency: 'PKR' }).format(context.parsed.y);
                                }
                                return label;
                            }
                        }
                    },
                    datalabels: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            drawBorder: false,
                            color: 'rgba(0, 0, 0, 0.05)'
                        },
                        ticks: {
                            callback: function(value) {
                                if (value >= 1000000) {
                                    return (value / 1000000).toFixed(1) + 'M';
                                } else if (value >= 1000) {
                                    return (value / 1000).toFixed(0) + 'K';
                                }
                                return value;
                            }
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                },
                interaction: {
                    intersect: false,
                    mode: 'index',
                },
            }
        });
    },

    renderProductionChart: function (canvasId, data) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }
        
        const labels = data.map(d => d.groupName);
        const percentages = data.map(d => d.percentage);
        
        const colors = [
            'rgba(255, 99, 132, 0.8)',
            'rgba(54, 162, 235, 0.8)',
            'rgba(255, 206, 86, 0.8)',
            'rgba(75, 192, 192, 0.8)',
            'rgba(153, 102, 255, 0.8)',
            'rgba(255, 159, 64, 0.8)',
            'rgba(199, 199, 199, 0.8)',
            'rgba(83, 102, 255, 0.8)',
            'rgba(40, 159, 64, 0.8)',
            'rgba(210, 199, 199, 0.8)'
        ];
        
        this.charts[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            plugins: [ChartDataLabels],
            data: {
                labels: labels,
                datasets: [{
                    data: percentages,
                    backgroundColor: colors.slice(0, data.length),
                    borderColor: 'white',
                    borderWidth: 2,
                    hoverOffset: 15
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '65%',
                plugins: {
                    datalabels: {
                        color: '#fff',
                        anchor: 'center',
                        align: 'center',
                        offset: 0,
                        font: {
                            weight: 'bold',
                            size: 14,
                            family: "'Inter', sans-serif"
                        },
                        formatter: (value) => {
                            return value > 0 ? value + '%' : '';
                        },
                        display: true
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            usePointStyle: true,
                            padding: 15,
                            font: {
                                size: 12
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                let label = context.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                label += context.parsed + '%';
                                return label;
                            }
                        }
                    }
                }
            }
        });
    }
};
