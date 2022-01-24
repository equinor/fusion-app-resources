import { FilterSection, FilterTypes } from '@equinor/fusion-components';
import PositionWithPersonnel from '../../../../../../models/PositionWithPersonnel';

const getFilterSections = (
    positions: PositionWithPersonnel[]
): FilterSection<PositionWithPersonnel>[] => {
    const uniqueDisciplines = positions
        .map((position) => position.basePosition.discipline)
        .filter((d, i, l) => l.indexOf(d) === i);
    const uniqueAdStatus = positions
        .map(
            (position) =>
                position.instances.find((i) => i.personnelDetails?.azureAdStatus)?.personnelDetails
                    ?.azureAdStatus || ''
        )
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
            key: 'filters',
            title: 'Filters',
            isCollapsible: true,
            filters: [
                {
                    key: 'ad-status-filter',
                    title: 'Person AD status',
                    type: FilterTypes.Checkbox,
                    getValue: (position) =>
                        position.instances.find((i) => i.personnelDetails?.azureAdStatus)
                            ?.personnelDetails?.azureAdStatus || '',
                    isVisibleWhenPaneIsCollapsed: true,
                    isCollapsible: true,
                    options: uniqueAdStatus.map((adStatus) => ({
                        key: adStatus,
                        label: adStatus || 'No Account',
                    })),
                },
                {
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
