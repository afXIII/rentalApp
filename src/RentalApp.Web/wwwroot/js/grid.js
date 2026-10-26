// Client side of the DataGrid view component (Views/Shared/Components/DataGrid).
// Fetches one page at a time from the JSON endpoint; the server does the filtering,
// sorting, and paging. State is mirrored in the URL so a reload or a shared link keeps it.
(() => {
  const statusCss = {
    Draft: 'text-bg-secondary',
    Submitted: 'text-bg-primary',
    UnderReview: 'text-bg-info',
    Returned: 'text-bg-warning',
    Approved: 'text-bg-success',
    Denied: 'text-bg-danger',
    Withdrawn: 'text-bg-dark',
  };
  const dateFormat = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

  function cell(column, row) {
    const td = document.createElement('td');
    const value = row[column.key];
    if (value === null || value === undefined || value === '') {
      td.innerHTML = '<span class="text-muted">&ndash;</span>';
    } else if (column.format === 'status') {
      const badge = document.createElement('span');
      badge.className = 'badge ' + (statusCss[value] || 'text-bg-secondary');
      badge.textContent = row.statusLabel || value;
      td.append(badge);
    } else if (column.format === 'date') {
      td.textContent = dateFormat.format(new Date(value));
    } else {
      td.textContent = value; // textContent, never innerHTML: values come from users
    }
    return td;
  }

  function linkFor(template, row) {
    return template.replace(/\{(\w+)\}/g, (_, key) => encodeURIComponent(row[key]));
  }

  function initGrid(root) {
    const config = JSON.parse(root.dataset.gridConfig);
    const tbody = root.querySelector('tbody');
    const pager = root.querySelector('[data-grid-pager]');
    const summary = root.querySelector('[data-grid-summary]');
    const pageSizeSelect = root.querySelector('[data-grid-page-size]');
    const filters = [...root.querySelectorAll('[data-grid-filter]')];
    const url = new URL(window.location.href);

    const state = {
      sort: url.searchParams.get('sort') || config.defaultSort,
      direction: url.searchParams.get('direction') || config.defaultDirection,
      page: Number(url.searchParams.get('page')) || 1,
      pageSize: Number(url.searchParams.get('pageSize')) || config.pageSize,
    };
    pageSizeSelect.value = String(state.pageSize);
    filters.forEach((f) => {
      if (url.searchParams.has(f.dataset.gridFilter)) f.value = url.searchParams.get(f.dataset.gridFilter);
    });

    function query() {
      const params = new URLSearchParams();
      filters.forEach((f) => { if (f.value) params.set(f.dataset.gridFilter, f.value); });
      params.set('sort', state.sort);
      params.set('direction', state.direction);
      params.set('page', state.page);
      params.set('pageSize', state.pageSize);
      return params;
    }

    function renderHeader() {
      root.querySelectorAll('th[data-sort]').forEach((th) => {
        const indicator = th.querySelector('.sort-indicator');
        const active = th.dataset.sort === state.sort;
        indicator.textContent = active ? (state.direction === 'asc' ? '▲' : '▼') : '';
        th.setAttribute('aria-sort', active ? (state.direction === 'asc' ? 'ascending' : 'descending') : 'none');
      });
    }

    function renderPager(total) {
      const pages = Math.max(1, Math.ceil(total / state.pageSize));
      pager.innerHTML = '';
      const add = (label, page, disabled, active) => {
        const li = document.createElement('li');
        li.className = 'page-item' + (disabled ? ' disabled' : '') + (active ? ' active' : '');
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'page-link';
        button.textContent = label;
        button.addEventListener('click', () => { state.page = page; load(); });
        li.append(button);
        pager.append(li);
      };
      add('‹', state.page - 1, state.page <= 1, false);
      const first = Math.max(1, Math.min(state.page - 2, pages - 4));
      for (let p = first; p <= Math.min(pages, first + 4); p++) add(String(p), p, false, p === state.page);
      add('›', state.page + 1, state.page >= pages, false);
    }

    async function load() {
      const params = query();
      history.replaceState(null, '', `${window.location.pathname}?${params}`);
      renderHeader();
      const res = await fetch(`${config.dataUrl}?${params}`, { headers: { Accept: 'application/json' } });
      if (!res.ok) {
        tbody.innerHTML = `<tr><td colspan="${config.columns.length}" class="text-danger text-center py-4">Could not load rows.</td></tr>`;
        return;
      }
      const { rows, total, page, pageSize } = await res.json();
      state.page = page;
      tbody.innerHTML = '';
      if (rows.length === 0) {
        const tr = document.createElement('tr');
        const td = document.createElement('td');
        td.colSpan = config.columns.length;
        td.className = 'text-center text-muted py-4';
        td.textContent = config.emptyText;
        tr.append(td);
        tbody.append(tr);
      }
      for (const row of rows) {
        const tr = document.createElement('tr');
        config.columns.forEach((column, i) => {
          const td = cell(column, row);
          if (i === 0 && config.rowLink) {
            const a = document.createElement('a');
            a.href = linkFor(config.rowLink, row);
            a.append(...td.childNodes);
            td.append(a);
          }
          tr.append(td);
        });
        tbody.append(tr);
      }
      const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
      summary.textContent = `Showing ${from}–${Math.min(page * pageSize, total)} of ${total}`;
      renderPager(total);
    }

    root.querySelectorAll('th[data-sort]').forEach((th) => {
      const toggle = () => {
        if (state.sort === th.dataset.sort) state.direction = state.direction === 'asc' ? 'desc' : 'asc';
        else { state.sort = th.dataset.sort; state.direction = 'asc'; }
        state.page = 1;
        load();
      };
      th.addEventListener('click', toggle);
      th.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggle(); } });
    });
    filters.forEach((f) => f.addEventListener('change', () => { state.page = 1; load(); }));
    pageSizeSelect.addEventListener('change', () => { state.pageSize = Number(pageSizeSelect.value); state.page = 1; load(); });

    load();
  }

  document.querySelectorAll('.data-grid').forEach(initGrid);
})();
