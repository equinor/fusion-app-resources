import { Button } from '@equinor/fusion-components';
import { FC, useCallback, useEffect, useMemo, useState } from 'react';
import Personnel from '../../../../../../../models/Personnel';
import styles from './styles.less';

type ToolbarFilterProps = {
    personnel: Personnel[];
    setFilteredPersonnel: (filteredPersonnel: Personnel[]) => void;
    filteredPersonnel: Personnel[];
};
type FilterKeys = 'equinorAccounts' | 'affiliateAccounts' | 'noMail';
type PersonnelFilter = (item: Personnel) => boolean;
type Filters = {
    filter: PersonnelFilter;
    isSelected?: boolean;
};
type SelectedFilters = Record<FilterKeys, Filters>;

const filterDefinitions: SelectedFilters = {
    affiliateAccounts: {
        filter: (p) => !p.mail?.includes('equinor'),
    },
    equinorAccounts: {
        filter: (p) => !!p.mail?.includes('equinor'),
    },
    noMail: {
        filter: (p) => !p.mail, // TODO: Add cusome mail
    },
};
const ToolbarFilter: FC<ToolbarFilterProps> = ({
    personnel,
    setFilteredPersonnel,
    filteredPersonnel,
}) => {
    const [selectedFilters, setSelectedFilters] = useState<SelectedFilters>(filterDefinitions);

    const personnelWithEquinorAccounts = useMemo(
        () => filteredPersonnel.filter(filterDefinitions.equinorAccounts.filter).length,
        [filteredPersonnel]
    );

    const personnelWithAffiliateAccounts = useMemo(
        () => filteredPersonnel.filter(filterDefinitions.affiliateAccounts.filter).length,
        [filteredPersonnel]
    );

    const personnelWithNoMail = useMemo(
        () => filteredPersonnel.filter(filterDefinitions.noMail.filter).length,
        [filteredPersonnel]
    );

    const filterPersonnel = useCallback(() => {
        const activeFilters = Object.values(selectedFilters)
            .filter((f) => f.isSelected)
            .map((f) => f.filter);

        if (activeFilters?.length === 0) {
            setFilteredPersonnel(personnel);
            return;
        }
        setFilteredPersonnel(personnel.filter((p) => activeFilters.every((filter) => filter(p))));
    }, [personnel, selectedFilters, setFilteredPersonnel]);

    const toggleSelected = useCallback(
        (filterKey: FilterKeys) => {
            const filter = selectedFilters[filterKey];
            setSelectedFilters({
                ...selectedFilters,
                [filterKey]: {
                    ...filter,
                    isSelected: !filter?.isSelected,
                },
            });
        },
        [selectedFilters, setSelectedFilters]
    );
    const onFilterEquinorAccounts = useCallback(
        () => toggleSelected('equinorAccounts'),
        [toggleSelected]
    );
    const onFilterAffiliateAccounts = useCallback(
        () => toggleSelected('affiliateAccounts'),
        [toggleSelected]
    );
    const onFilterNoMail = useCallback(() => toggleSelected('noMail'), [toggleSelected]);

    useEffect(() => {
        filterPersonnel();
    }, [selectedFilters]);

    return (
        <div className={styles.toolbar}>
            <div className={styles.toolbarItem}>
                <Button
                    outlined={!selectedFilters.equinorAccounts.isSelected}
                    onClick={onFilterEquinorAccounts}
                    disabled={personnelWithEquinorAccounts === 0}
                >
                    Equinor accounts ({personnelWithEquinorAccounts})
                </Button>
            </div>
            <div className={styles.toolbarItem}>
                <Button
                    outlined={!selectedFilters.affiliateAccounts.isSelected}
                    onClick={onFilterAffiliateAccounts}
                    disabled={personnelWithAffiliateAccounts === 0}
                >
                    Affiliate accounts ({personnelWithAffiliateAccounts})
                </Button>
            </div>
            <div className={styles.toolbarItem}>
                <Button
                    outlined={!selectedFilters.noMail.isSelected}
                    onClick={onFilterNoMail}
                    disabled={personnelWithNoMail === 0}
                >
                    Has no mail ({personnelWithNoMail})
                </Button>
            </div>
        </div>
    );
};

export default ToolbarFilter;
