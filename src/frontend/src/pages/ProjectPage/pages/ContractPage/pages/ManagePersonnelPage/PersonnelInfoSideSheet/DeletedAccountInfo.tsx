import { createStyles, makeStyles } from '@equinor/fusion-react-styles';
import Tooltip from '@equinor/fusion-react-tooltip';
import { FC, useCallback, useState } from 'react';
import Personnel from '../../../../../../../models/Personnel';
import ReplaceUserSideSheet from '../ReplaceUserSideSheet';
import useReplacePersonAccess from './useReplacePersonAccess';
import Button from '@equinor/fusion-react-button';

const useStyles = makeStyles((theme) =>
    createStyles({
        container: {
            border: '1px solid',
            borderColor: theme.colors.infographic.primary__energy_red_100.getVariable('color'),
            backgroundColor: theme.colors.infographic.primary__energy_red_13.getVariable('color'),
            borderRadius: '4px',
            display: 'flex',
            flexDirection: 'column',
            padding: '1rem',
            margin: '1rem 2rem',
        },
        title: {
            fontSize: '18px',
            paddingBottom: '1rem',
        },
        infoContainer: {
            display: 'flex',
            flexDirection: 'column',
        },
        infoText: {
            paddingBottom: '1rem',
        },
        buttonContainer: {
            width: 'max-content',
        },
    })
);

type DeletedAccountInfoProps = {
    person: Personnel;
};
const DeletedAccountInfo: FC<DeletedAccountInfoProps> = ({ person }) => {
    const styles = useStyles();
    const [showReplaceUserSideSheet, setShowReplaceUserSideSheet] = useState<boolean>(false);

    const canReplace = useReplacePersonAccess(person);

    const openReplaceUserSideSheet = useCallback(
        () => canReplace && setShowReplaceUserSideSheet(true),
        [setShowReplaceUserSideSheet, canReplace]
    );

    return (
        <>
            <div className={styles.container}>
                <div className={styles.title}>Deleted account detected</div>
                <div className={styles.infoContainer}>
                    <div className={styles.infoText}>
                        This person is referencing to a account instance that has been deleted from
                        Azure AD.
                    </div>
                    <div>
                        <Tooltip
                            content={`${
                                canReplace
                                    ? 'Click to get more information and to update person reference'
                                    : !person.azureUniquePersonId
                                    ? 'Unable to replace: AzureUniquerId is required'
                                    : 'No access'
                            }`}
                        >
                            <div className={styles.buttonContainer}>
                                <Button onClick={openReplaceUserSideSheet} disabled={!canReplace}>
                                    Update person reference
                                </Button>
                            </div>
                        </Tooltip>
                    </div>
                </div>
            </div>
            <ReplaceUserSideSheet
                isOpen={showReplaceUserSideSheet}
                person={person}
                setIsOpen={setShowReplaceUserSideSheet}
            />
        </>
    );
};
export default DeletedAccountInfo;
