import { createStyles, makeStyles } from '@equinor/fusion-react-styles';

const useSavePersonnelError = makeStyles((theme) =>
    createStyles({
        container: {
            fontSize: '14px',
        },
        inputError: {
            paddingTop: '1rem',
            display: 'flex',
        },
        preferredContactMail: {
            minWidth: "13rem"
        },
        errorDescription: {
            color: theme.colors.infographic.primary__energy_red_100.value.rgba,
        },
    })
);
export default useSavePersonnelError;
