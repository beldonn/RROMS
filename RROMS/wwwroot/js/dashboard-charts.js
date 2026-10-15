// Renders the Admin dashboard charts from JSON embedded by Pages/Admin/Dashboard.cshtml
// (window.rromsDashboardData) using Chart.js, loaded via CDN in that page.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var data = window.rromsDashboardData;
        if (!data || typeof Chart === 'undefined') return;

        var gold = '#bd8b2e';
        var herb = '#3e5641';
        var wine = '#6b2737';
        var ink = '#211d19';

        var salesEl = document.getElementById('chartSales');
        if (salesEl) {
            new Chart(salesEl, {
                type: 'line',
                data: {
                    labels: data.salesLast7Days.map(function (d) { return d.label; }),
                    datasets: [{
                        label: 'Revenue',
                        data: data.salesLast7Days.map(function (d) { return d.revenue; }),
                        borderColor: gold,
                        backgroundColor: 'rgba(189,139,46,0.12)',
                        fill: true,
                        tension: 0.3
                    }]
                },
                options: { plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } }
            });
        }

        var hourEl = document.getElementById('chartReservationsByHour');
        if (hourEl) {
            new Chart(hourEl, {
                type: 'bar',
                data: {
                    labels: data.reservationsByHour.map(function (d) { return d.label; }),
                    datasets: [{ label: 'Reservations', data: data.reservationsByHour.map(function (d) { return d.count; }), backgroundColor: herb }]
                },
                options: { plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } }
            });
        }

        var popularEl = document.getElementById('chartPopularItems');
        if (popularEl) {
            new Chart(popularEl, {
                type: 'bar',
                data: {
                    labels: data.topMenuItems.map(function (d) { return d.label; }),
                    datasets: [{ label: 'Quantity sold', data: data.topMenuItems.map(function (d) { return d.qty; }), backgroundColor: gold }]
                },
                options: { indexAxis: 'y', plugins: { legend: { display: false } }, scales: { x: { beginAtZero: true, ticks: { precision: 0 } } } }
            });
        }

        var tableEl = document.getElementById('chartTableUtilization');
        if (tableEl) {
            new Chart(tableEl, {
                type: 'doughnut',
                data: {
                    labels: data.tableBookingFrequency.map(function (d) { return d.label; }),
                    datasets: [{ data: data.tableBookingFrequency.map(function (d) { return d.count; }), backgroundColor: [gold, herb, wine, ink, '#8f691f', '#2c3f30', '#a35a68', '#5a5044'] }]
                },
                options: { plugins: { legend: { position: 'bottom', labels: { boxWidth: 12 } } } }
            });
        }
    });
})();
