import { FilterSection, FilterTypes } from '@equinor/fusion-components';
import Personnel, { azureAdStatus } from '../../../../../../models/Personnel';
import {
    AzureAdStatusColor,
    AzureAdStatusTextFormat,
} from '../../components/AzureAdStatusIndicator';

const getFilterSections = (personnel: Personnel[]): FilterSection<Personnel>[] => {
    const uniqueAdStatus = personnel
        .map((p) => (p.isDeleted ? 'DeletedAccount' : p?.azureAdStatus || 'NoAccount'))
        .filter((d, i, l) => l.indexOf(d) === i);

    const uniqueDisciplines = personnel
        .reduce<string[]>((arr, p) => {
            return arr.concat(p.disciplines?.map((d) => d.name));
        }, [])
        .filter((d, i, l) => l.indexOf(d) === i);

    return [
        {
            key: 'search',
            title: 'Search',
            filters: [
                {
                    key: 'search-filter',
                    type: FilterTypes.Search,
                    title: '',
                    getValue: (p) =>
                        p.name +
                        (p.firstName || '') +
                        (p.lastName || '') +
                        p.mail +
                        p.phoneNumber +
                        (p.disciplines?.map((d) => d.name).join(' ') || '') +
                        (p.upn || '') +
                        p.azureUniquePersonId,
                },
            ],
        },
        {
            key: 'filters',
            title: 'Filters',
            isCollapsible: true,
            filters: [
                {
                    key: 'discipline-filter',
                    title: 'Disciplines',
                    type: FilterTypes.Checkbox,
                    getValue: (p) => p.disciplines?.map((d) => d.name).join(' ') || '',
                    isVisibleWhenPaneIsCollapsed: true,
                    isCollapsible: true,
                    options: uniqueDisciplines.map((discipline) => ({
                        key: discipline,
                        label: discipline || '(none)',
                    })),
                },
                {
                    key: 'azureAdStatus',
                    title: 'AD Status',
                    type: FilterTypes.Checkbox,
                    getValue: (p) =>
                        p.isDeleted ? 'DeletedAccount' : p?.azureAdStatus || 'NoAccount',
                    isVisibleWhenPaneIsCollapsed: true,
                    isCollapsible: true,

                    options: uniqueAdStatus.map((ads: azureAdStatus) => ({
                        key: ads,
                        label: AzureAdStatusTextFormat(ads),
                        color: AzureAdStatusColor(ads),
                    })),
                },
            ],
        },
    ];
};

export default getFilterSections;
