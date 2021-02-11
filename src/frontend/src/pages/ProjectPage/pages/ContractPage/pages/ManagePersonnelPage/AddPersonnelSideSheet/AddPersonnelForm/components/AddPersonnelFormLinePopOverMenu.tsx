
import { usePopoverRef, MoreIcon } from '@equinor/fusion-components';
import { FC, useCallback } from 'react';
import ManagePersonnelToolBar, {
    IconButtonProps,
} from '../../../components/ManagePersonnelToolBar';
import PersonnelLine from '../../models/PersonnelLine';

type PopOverMenuProps = {
    person: PersonnelLine;
    onDeletePerson: (person: PersonnelLine) => void;
};

export const PopOverMenu: FC<PopOverMenuProps> = ({ person, onDeletePerson }) => {
    const deleteButton = useCallback((): IconButtonProps => {
        if (person.created) return { disabled: true };

        return { onClick: () => onDeletePerson(person) };
    }, [onDeletePerson, person]);

    const [popoverRef] = usePopoverRef<HTMLDivElement>(
        <ManagePersonnelToolBar deleteButton={deleteButton()} />,
        {
            justify: 'center',
        }
    );

    return (
        <div ref={popoverRef}>
            <MoreIcon />
        </div>
    );
};
