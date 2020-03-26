import { FilterSection, FilterTypes, WarningIcon } from '@equinor/fusion-components';
import Personnel, { azureAdStatus } from '../../../../../../models/Personnel';
import { AzureAdStatusTextFormat, AzureAdStatusColor } from './components/AzureAdStatus';

const getFilterSections = (personnel: Personnel[]): FilterSection<Personnel>[] => {
    const uniqueAdStatus = personnel
        .map(p => p?.azureAdStatus || 'NoAccount')
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
                    getValue: p =>
                        p.name +
                        (p.firstName || '') +
                        (p.lastName || '') +
                        p.mail +
                        p.phoneNumber +
                        (p.disciplines?.map(d => d.name).join(' ') || ''),
                },
            ],
        },
        {
            key: 'filters',
            title: 'Filters',
            isCollapsible: true,
            filters: [
                {
                    key: 'azureAdStatus',
                    title: 'AD Status',
                    type: FilterTypes.Checkbox,
                    getValue: p => p?.azureAdStatus || 'NoAccount',
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
