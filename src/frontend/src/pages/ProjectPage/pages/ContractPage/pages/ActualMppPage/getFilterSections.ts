import { FilterSection, FilterTypes } from '@equinor/fusion-components';
import { Position } from '@equinor/fusion';

const getFilterSections = (positions: Position[]): FilterSection<Position>[] => {
    const uniqueBasePositions = positions
        .map(position => position.basePosition.name)
        .filter((d, i, l) => l.indexOf(d) === i);

    return [
        {
            key: 'search',
            title: 'Search',
            filters: [
                {
                    key: 'search-filter',
                    type: FilterTypes.Search,
                    title: 'Search',
                    getValue: position =>
                        position.id +
                        position.name +
                        position.basePosition.name +
                        (position.instances.find(i => i.workload)?.workload || '') +
                        (position.instances.find(i => i.assignedPerson?.name)?.assignedPerson
                            ?.name || ''),
                },
            ],
        },
        {
            key: 'filters',
            title: 'Filters',
            isCollapsible: true,
            filters: [
                {
                    key: 'base-positions',
                    title: 'Base positions',
                    type: FilterTypes.Checkbox,
                    getValue: position => position.basePosition.name,
                    isVisibleWhenPaneIsCollapsed: true,
                    isCollapsible: true,
                    options: uniqueBasePositions.map(basePosition => ({
                        key: basePosition,
                        label: basePosition || '(none)',
                    })),
                },
            ],
        },
    ];
};

export default getFilterSections;
