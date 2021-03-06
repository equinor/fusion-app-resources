import { FilterSection, FilterTypes } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';

const getFilterSections = (requests: PersonnelRequest[]): FilterSection<PersonnelRequest>[] => {
    const uniqueDisciplines = requests
        .map(request => request.position?.basePosition?.discipline || '')
        .filter((d, i, l) => l.indexOf(d) === i);

    const uniqueStatus = requests
        .map(request => request.state)
        .filter((d, i, l) => l.indexOf(d) === i);

    return [
        {
            key: 'search-section',
            title: '',
            filters: [
                {
                    key: 'search-filter',
                    type: FilterTypes.Search,
                    title: 'Search',
                    getValue: item =>
                        item.id +
                        (item.person?.name || '') +
                        (item.state + item.position?.name || '') +
                        (item.position?.basePosition?.name || ''),
                },
            ],
        },
        {
            key: 'filters',
            title: 'Filters',
            isCollapsible: true,
            filters: [
                {
                    key: 'status',
                    title: 'Status',
                    type: FilterTypes.Checkbox,
                    getValue: request => request.state,
                    isVisibleWhenPaneIsCollapsed: true,
                    isCollapsible: true,
                    options: uniqueStatus.map(status => ({
                        key: status,
                        label: status,
                    })),
                },
                {
                    key: 'disciplines',
                    title: 'Disciplines',
                    type: FilterTypes.Checkbox,
                    getValue: request => request.position?.basePosition?.discipline || '',
                    isVisibleWhenPaneIsCollapsed: true,
                    isCollapsible: true,
                    options: uniqueDisciplines.map(discipline => ({
                        key: discipline,
                        label: discipline || '(none)',
                    })),
                },
            ],
        },
    ];
};

export default getFilterSections;
