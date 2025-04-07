import { Global, useMantineTheme } from '@mantine/core';

export function GridGlobalStyles() {
    const theme = useMantineTheme();

    const colors = theme.fn.variant({ variant: 'light', color: theme.primaryColor });

    return (
        <Global
            styles={() => ({
                '.w2ui-grid-records': {
                    overscrollBehavior: 'contain', // fix for not scrolling the grid container
                },
                '.w2ui-grid': {
                    border: 'unset',
                    borderRadius: 'unset',
                },
                '.w2ui-grid .w2ui-grid-body table .w2ui-head': {
                    color: 'unset',
                    backgroundImage: 'unset',
                    fontWeight: 'bold',
                    borderRight: 'unset',
                    borderBottom: 'unset',
                },
                '.w2ui-reset, .w2ui-reset table tr th, .w2ui-reset table tr td': {
                    fontFamily: 'unset',
                    fontSize: '0.875rem',
                },
                '.w2ui-grid .w2ui-grid-body': {
                    backgroundColor: 'unset',
                },
                '.w2ui-grid .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd, .w2ui-grid .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd, .w2ui-grid .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even, .w2ui-grid .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even': {
                    backgroundColor: 'unset',
                    borderBottom: 'unset',
                    borderTop: 'unset',
                },
                '.w2ui-grid .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd:hover, .w2ui-grid .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd:hover, .w2ui-grid .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd.w2ui-record-hover, .w2ui-grid .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd.w2ui-record-hover, .w2ui-grid .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even:hover, .w2ui-grid .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even:hover, .w2ui-grid .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even.w2ui-record-hover, .w2ui-grid .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even.w2ui-record-hover': {
                    backgroundColor: 'unset',
                },
                '.w2ui-grid .w2ui-grid-body .w2ui-grid-columns, .w2ui-grid .w2ui-grid-body .w2ui-grid-fcolumns': {
                    boxShadow: 'unset',
                },
                '.w2ui-grid .w2ui-grid-body table td': {
                    borderRight: 'unset',
                },
                '.w2ui-grid .w2ui-grid-body .w2ui-grid-scroll1': {
                    backgroundColor: 'unset',
                    borderTop: 'unset',
                    borderRight: 'unset',
                },
                '.w2ui-grid .w2ui-grid-body div.w2ui-col-header > div.w2ui-sort-down': {
                    marginTop: '0.3rem',
                },
                '.w2ui-grid .w2ui-grid-body div.w2ui-col-header > div.w2ui-sort-up': {
                    marginTop: 0,
                },
                '.w2ui-grid.w2grid-light.w2grid-column-borders .w2ui-grid-body table td:not(.w2ui-head):not(:last-child), .w2ui-grid.w2grid-light.w2grid-column-borders .w2ui-grid-body table td.w2ui-head:not(:nth-last-child(2)), .w2ui-grid.w2grid-light.w2grid-column-borders .w2ui-grid-body table td.w2ui-head.w2ui-col-select': {
                    borderRight: '0.0625rem solid rgb(222, 226, 230)',
                },
                '.w2ui-grid.w2grid-dark.w2grid-column-borders .w2ui-grid-body table td:not(.w2ui-head):not(:last-child), .w2ui-grid.w2grid-dark.w2grid-column-borders .w2ui-grid-body table td.w2ui-head:not(:nth-last-child(2)), .w2ui-grid.w2grid-dark.w2grid-column-borders .w2ui-grid-body table td.w2ui-head.w2ui-col-select': {
                    borderRight: '0.0625rem solid rgb(55, 58, 64)',
                },
                '.w2ui-grid.w2grid-light.w2grid-border': {
                    border: '0.0625rem solid rgb(222, 226, 230)',
                },
                '.w2ui-grid.w2grid-dark.w2grid-border': {
                    border: '0.0625rem solid rgb(55, 58, 64)',
                },
                '.w2ui-grid.w2grid-light.w2grid-row-borders .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd, .w2ui-grid.w2grid-light.w2grid-row-borders .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd, .w2ui-grid.w2grid-light.w2grid-row-borders .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even, .w2ui-grid.w2grid-light.w2grid-row-borders .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even': {
                    borderTop: '0.0625rem solid #dee2e6',
                },
                '.w2ui-grid.w2grid-dark.w2grid-row-borders .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd, .w2ui-grid.w2grid-dark.w2grid-row-borders .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd, .w2ui-grid.w2grid-dark.w2grid-row-borders .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even, .w2ui-grid.w2grid-dark.w2grid-row-borders .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even': {
                    borderTop: '0.0625rem solid rgb(55, 58, 64)',
                },
                '.w2ui-grid.w2grid-light.w2grid-stripped .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd, .w2ui-grid.w2grid-light.w2grid-stripped .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd': {
                    backgroundColor: '#f8f9fa',
                },
                '.w2ui-grid.w2grid-dark.w2grid-stripped .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd, .w2ui-grid.w2grid-dark.w2grid-stripped .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd': {
                    backgroundColor: 'rgb(37, 38, 43)',
                },
                '.w2ui-grid.w2grid-light.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd:hover, .w2ui-grid.w2grid-light.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd:hover, .w2ui-grid.w2grid-light.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd.w2ui-record-hover, .w2ui-grid.w2grid-light.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd.w2ui-record-hover, .w2ui-grid.w2grid-light.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even:hover, .w2ui-grid.w2grid-light.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even:hover, .w2ui-grid.w2grid-light.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even.w2ui-record-hover, .w2ui-grid.w2grid-light.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even.w2ui-record-hover': {
                    backgroundColor: '#f1f3f5',
                },
                '.w2ui-grid.w2grid-dark.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd:hover, .w2ui-grid.w2grid-dark.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd:hover, .w2ui-grid.w2grid-dark.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-records table tr.w2ui-odd.w2ui-record-hover, .w2ui-grid.w2grid-dark.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-odd.w2ui-record-hover, .w2ui-grid.w2grid-dark.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even:hover, .w2ui-grid.w2grid-dark.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even:hover, .w2ui-grid.w2grid-dark.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-records table tr.w2ui-even.w2ui-record-hover, .w2ui-grid.w2grid-dark.w2grid-highlight-hover .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-even.w2ui-record-hover': {
                    backgroundColor: 'rgb(44, 46, 51)',
                },
                '.w2ui-grid.w2grid-light .w2ui-grid-body .w2ui-grid-records table tr.w2ui-selected, .w2ui-grid.w2grid-light .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-selected, .w2ui-grid.w2grid-light .w2ui-grid-body .w2ui-grid-records table tr td.w2ui-selected, .w2ui-grid.w2grid-light .w2ui-grid-body .w2ui-grid-frecords table tr td.w2ui-selected': {
                    color: `${colors.color} !important`,
                    backgroundColor: `${colors.background} !important`,
                },
                '.w2ui-grid.w2grid-dark .w2ui-grid-body .w2ui-grid-records table tr.w2ui-selected, .w2ui-grid.w2grid-dark .w2ui-grid-body .w2ui-grid-frecords table tr.w2ui-selected, .w2ui-grid.w2grid-dark .w2ui-grid-body .w2ui-grid-records table tr td.w2ui-selected, .w2ui-grid.w2grid-dark .w2ui-grid-body .w2ui-grid-frecords table tr td.w2ui-selected': {
                    color: `${colors.color} !important`,
                    backgroundColor: `${colors.background} !important`,
                },
            })}
        />
    );
}

