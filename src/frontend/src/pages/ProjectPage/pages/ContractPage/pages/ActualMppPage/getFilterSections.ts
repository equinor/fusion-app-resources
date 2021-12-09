import { FilterSection, FilterTypes } from '@equinor/fusion-components';
import { Position } from '@equinor/fusion';

const getFilterSections = (positions: Position[]): FilterSection<Position>[] => {
    const uniqueDisciplines = positions
        .map((position) => position.basePosition.discipline)
        .filter((d, i, l) => l.indexOf(d) === i);

    return [
        {
            key: 'search',
            title: 'Search',
            filters: [
                {
                    id: 'search-filter',
                    key: 'search-filter',
                    type: FilterTypes.Search,
                    title: 'Search',
                    getValue: (position) =>
                        position.id +
                        position.name +
                        position.basePosition.name +
                        (position.instances.find((i) => i.workload)?.workload || '') +
                        (position.instances.find((i) => i.assignedPerson?.name)?.assignedPerson
                            ?.name || '') +
                        (position.instances.find((i) => i.assignedPerson?.mail)?.assignedPerson
                            ?.mail || ''),
                },
            ],
        },
        {
            id: 'filter-section',
            key: 'filters',
            title: 'Filters',
            isCollapsible: true,
            filters: [
                {
                    id: 'disciplines-filter',
                    key: 'discipline-filter',
                    title: 'Disciplines',
                    type: FilterTypes.Checkbox,
                    getValue: (position) => position.basePosition.discipline,
                    isVisibleWhenPaneIsCollapsed: true,
                    isCollapsible: true,
                    options: uniqueDisciplines.map((discipline) => ({
                        key: discipline,
                        label: discipline || '(none)',
                    })),
                },
            ],
        },
    ];
};

export default getFilterSections;
