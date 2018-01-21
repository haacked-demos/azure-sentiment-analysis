// Please visit http://go.microsoft.com/fwlink/?LinkID=761099&clcid=0x409 for more information on settting up Github Webhooks
module.exports = function (context, data) {
    context.log('GitHub Webhook triggered!', data.comment.body + ' on issue ' + data.issue.title);
    context.res = { body: 'New GitHub Comment: ' + data.comment.body + ' on issue ' + data.issue.title };
    context.done();
};
