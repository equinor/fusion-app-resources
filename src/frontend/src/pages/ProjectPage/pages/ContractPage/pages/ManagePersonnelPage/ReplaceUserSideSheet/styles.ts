import { createStyles, makeStyles } from '@equinor/fusion-react-styles';

export const useReplaceUserSideSheetStyles = makeStyles(
    (theme) =>
        createStyles({
            container: {
                padding: '1rem 2rem',
                display: 'flex',
                flexDirection: 'column',
            },
            infoContainer: { padding: '.5rem 0' },
            accountComparison: {
                display: 'flex',
                flexDirection: 'column',
            },
            dataField: {
                display: 'flex',
                flexDirection: 'column',
            },
            dataTitle: {
                fontWeight: '500',
                padding: '.5rem 0',
            },
            dataContent: {
                paddingLeft: '1rem',
                paddingBottom: '.5rem',
            },
            oldRef: {
                textDecoration: 'line-through',
                color: theme.colors.interactive.danger__text.getVariable('color'),
            },
            newRef: {
                color: theme.colors.interactive.success__text.getVariable('color'),
            },
            updateButton: {
                display: 'flex',
                flexDirection: 'row-reverse',
                justifyContent: 'space-between',
                alignItems: 'center',
            },
            elevatedAccessInfo: {
                backgroundColor: theme.colors.ui.background__info.getVariable('color'),
                borderRadius: '4px',
                padding: '0 1rem',
                display: 'flex',
                alignItems: 'center',
                height: '2rem',
            },
        }),
    {
        name: 'replace-user-side-sheet-styles',
    }
);
